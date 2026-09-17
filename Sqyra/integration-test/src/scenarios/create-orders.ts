import {
  aggregate,
  caseWhen,
  column,
  columnExpression,
  deleteFrom,
  insertInto,
  select,
  sqlType,
  update,
  values,
  exprIsNull,
  exprTypeString,
} from "sqyra";
import { scalar } from "../context.js";
import { lit } from "sqyra";
import { defineIntegrationTables } from "../tables.js";
import type { Scenario } from "./types.js";

export const createOrdersScenario: Scenario = {
  source: "ScCreateOrders",
  async run(context) {
    const { order, customer, user, company } = defineIntegrationTables(context.dialect);
    const nameQuery = select({
      CustomerId: customer.CustomerId,
      CustomerTypeId: caseWhen(exprIsNull({ test: columnExpression(user.UserId), not: true }))
        .then(1)
        .when(exprIsNull({ test: columnExpression(company.CompanyId), not: true }))
        .then(2)
        .else(null),
      Name: caseWhen(exprIsNull({ test: columnExpression(user.UserId), not: true }))
        .then(user.FirstName.concat(" ").concat(user.LastName))
        .when(exprIsNull({ test: columnExpression(company.CompanyId), not: true }))
        .then(company.CompanyName)
        .else("-"),
    })
      .from(customer)
      .leftJoin(user)
      .leftJoin(company);
    const numbers = values(
      Array.from({ length: 10 }, (_, index) => [index + 1]),
      "numbers",
      { Num: column(sqlType.int32) },
    );
    const note = lit("Notes for ")
      .concat(nameQuery.Name)
      .concat(" No:")
      .concat(numbers.Num.cast(exprTypeString({ size: 5, isUnicode: false, isText: false })));
    const orderSource = select({ CustomerId: nameQuery.CustomerId, Notes: note })
      .from(nameQuery)
      .crossJoin(numbers)
      .orderBy(nameQuery.CustomerId, numbers.Num);
    await context.execute(insertInto(order).from(orderSource, "CustomerId", "Notes"));
    const count = async () =>
      Number(scalar(await context.query(select(aggregate("COUNT", 1)).from(order))));
    const inserted = await count();
    const customers = Number(
      scalar(await context.query(select(aggregate("COUNT", 1)).from(customer))),
    );
    if (inserted !== customers * 10)
      throw new Error(`Expected 10 orders per customer (${customers * 10}), received ${inserted}.`);
    const secondOrder = order.as("orderSub2");
    const bySeven = select(secondOrder.OrderId)
      .from(secondOrder)
      .where(secondOrder.OrderId.modulo(7).eq(0));
    if (context.dialect === "mysql-oracle") {
      const nested = select(bySeven.OrderId).from(bySeven);
      await context.execute(deleteFrom(order).where(order.OrderId.inQuery(nested)));
    } else await context.execute(deleteFrom(order).where(order.OrderId.inQuery(bySeven)));
    const afterSeven = await count();
    if (afterSeven > inserted || afterSeven < 0)
      throw new Error("Deleting every seventh order increased the row count.");
    const joinedPredicate = order.CustomerId.modulo(7).add(1).eq(1);
    const afterJoin =
      context.dialect === "sqlite"
        ? await context.execute(deleteFrom(order).where(joinedPredicate))
        : await context.execute(
            deleteFrom(order)
              .from(order)
              .innerJoin(nameQuery, nameQuery.CustomerId.eq(order.CustomerId))
              .where(joinedPredicate).ast,
          );
    const remaining = await count();
    if (remaining > afterSeven || remaining < 0 || afterJoin.affectedRows < 0)
      throw new Error("Joined order delete produced an invalid count.");
    await context.execute(
      update(order)
        .set({ Notes: order.Notes.concat(" (Updated 17)") })
        .where(order.OrderId.modulo(17).eq(0)),
    );
    const predicate19 = order.OrderId.modulo(19).eq(0);
    await context.execute(
      context.dialect === "sqlite"
        ? update(order)
            .set({ Notes: order.Notes.concat(" (Updated 19)") })
            .where(predicate19)
        : update(order)
            .set({ Notes: order.Notes.concat(" (Updated 19)") })
            .from(order)
            .where(predicate19),
    );
    const predicate23 = order.OrderId.modulo(23).eq(0);
    await context.execute(
      (context.dialect === "sqlite"
        ? update(order)
            .set({ Notes: order.Notes.concat(" (Updated 23)") })
            .where(predicate23)
        : update(order)
            .set({ Notes: order.Notes.concat(" (Updated 23)") })
            .from(order)
            .innerJoin(nameQuery, nameQuery.CustomerId.eq(order.CustomerId))
            .where(predicate23)
      ).ast,
    );
    const final = await count();
    if (final !== remaining) throw new Error("Order updates unexpectedly changed the row count.");
  },
};
