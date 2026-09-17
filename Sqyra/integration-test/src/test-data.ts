import { readFile } from "node:fs/promises";

export interface UserData {
  readonly external_id: string;
  readonly first_name: string;
  readonly last_name: string;
  readonly email: string;
}
export interface CompanyData {
  readonly external_id: string;
  readonly name: string;
}
async function readJson<T>(name: string): Promise<ReadonlyArray<T>> {
  return JSON.parse(
    await readFile(new URL(`../data/${name}`, import.meta.url), "utf8"),
  ) as ReadonlyArray<T>;
}
export const readUsers = (): Promise<ReadonlyArray<UserData>> => readJson<UserData>("users.json");
export const readCompanies = (): Promise<ReadonlyArray<CompanyData>> =>
  readJson<CompanyData>("company.json");
export function chunks<T>(items: ReadonlyArray<T>, size: number): ReadonlyArray<ReadonlyArray<T>> {
  const result: T[][] = [];
  for (let index = 0; index < items.length; index += size)
    result.push(items.slice(index, index + size));
  return result;
}
