using System.Collections.Generic;

namespace SqExpress.SqlTranspiler.Blazor.Pages
{
    internal static class ShowcaseCatalog
    {
        public static readonly IReadOnlyList<ShowcaseSample> All = new[]
        {
            new ShowcaseSample(
                "ranked-revenue",
                "1. Ranked Customer Revenue",
                "Multiple CTEs, joins, aggregation, and ROW_NUMBER ranking over revenue totals.",
                """
                WITH revenue_by_customer AS (
                    SELECT o.CustomerId, SUM(o.TotalAmount) AS Revenue
                    FROM dbo.Orders o
                    WHERE o.OrderDate >= @fromDate
                    GROUP BY o.CustomerId
                ),
                ranked_customers AS (
                    SELECT c.CustomerId, c.CustomerName, r.Revenue,
                           ROW_NUMBER() OVER(ORDER BY r.Revenue DESC) AS RevenueRank
                    FROM revenue_by_customer r
                    INNER JOIN dbo.Customers c ON c.CustomerId = r.CustomerId
                )
                SELECT rc.CustomerId, rc.CustomerName, rc.Revenue, rc.RevenueRank
                FROM ranked_customers rc
                WHERE rc.RevenueRank <= 10
                ORDER BY rc.RevenueRank;
                """),
            new ShowcaseSample(
                "list-filter-cte",
                "2. List Parameter CTE",
                "List variables in IN(@userIds), filtered CTEs, and descriptor generation for joined tables.",
                """
                WITH base_users AS (
                    SELECT u.UserId, u.Name, u.TeamId
                    FROM dbo.Users u
                    WHERE u.UserId IN(@userIds)
                )
                SELECT bu.UserId, bu.Name, t.TeamName
                FROM base_users bu
                LEFT JOIN dbo.Teams t ON t.TeamId = bu.TeamId
                ORDER BY bu.Name;
                """),
            new ShowcaseSample(
                "regional-window",
                "3. Regional Window Totals",
                "Nested derived tables plus COUNT and SUM OVER() to show analytic projection support.",
                """
                SELECT q.RegionId, q.RegionName, q.ActiveUsers, q.RegionalTotal
                FROM (
                    SELECT r.RegionId, r.RegionName,
                           COUNT(u.UserId) AS ActiveUsers,
                           SUM(COUNT(u.UserId)) OVER() AS RegionalTotal
                    FROM dbo.Regions r
                    LEFT JOIN dbo.Users u ON u.RegionId = r.RegionId
                    WHERE u.IsActive = 1
                    GROUP BY r.RegionId, r.RegionName
                ) q
                WHERE q.ActiveUsers > 0
                ORDER BY q.ActiveUsers DESC;
                """),
            new ShowcaseSample(
                "cross-apply-latest-order",
                "4. CROSS APPLY Latest Order",
                "Correlated CROSS APPLY with TOP 1 to showcase lateral table-source translation.",
                """
                SELECT c.CustomerId, c.CustomerName, lastOrder.OrderId, lastOrder.OrderDate, lastOrder.TotalAmount
                FROM dbo.Customers c
                CROSS APPLY (
                    SELECT TOP 1 o.OrderId, o.OrderDate, o.TotalAmount
                    FROM dbo.Orders o
                    WHERE o.CustomerId = c.CustomerId
                    ORDER BY o.OrderDate DESC
                ) lastOrder;
                """),
            new ShowcaseSample(
                "outer-apply-open-ticket",
                "5. OUTER APPLY Ticket Snapshot",
                "OUTER APPLY keeps the left row even when the correlated subquery finds no match.",
                """
                SELECT u.UserId, u.Name, nextTicket.TicketId, nextTicket.Priority
                FROM dbo.Users u
                OUTER APPLY (
                    SELECT TOP 1 t.TicketId, t.Priority
                    FROM dbo.Tickets t
                    WHERE t.AssignedUserId = u.UserId
                      AND t.Status = 'OPEN'
                    ORDER BY t.Priority DESC, t.CreatedAt
                ) nextTicket
                WHERE u.IsActive = 1;
                """),
            new ShowcaseSample(
                "paged-count-over",
                "6. Paged Results With Total Rows",
                "OFFSET/FETCH combined with COUNT(1) OVER() to demonstrate pagination-friendly SQL.",
                """
                SELECT o.OrderId, o.CustomerId, o.TotalAmount, COUNT(1) OVER() AS TotalRows
                FROM dbo.Orders o
                WHERE o.OrderDate >= @fromDate
                ORDER BY o.OrderDate DESC
                OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;
                """),
            new ShowcaseSample(
                "update-from-derived",
                "7. UPDATE From Aggregated Source",
                "UPDATE ... FROM with a grouped derived table and typed parameters.",
                """
                UPDATE u
                SET u.LastOrderDate = src.LastOrderDate,
                    u.IsVip = src.IsVip
                FROM dbo.Users u
                INNER JOIN (
                    SELECT o.UserId, MAX(o.OrderDate) AS LastOrderDate, CAST(1 AS bit) AS IsVip
                    FROM dbo.Orders o
                    WHERE o.TotalAmount >= @vipThreshold
                    GROUP BY o.UserId
                ) src ON src.UserId = u.UserId;
                """),
            new ShowcaseSample(
                "delete-from-join",
                "8. DELETE With Joined Source",
                "Joined DELETE from a grouped staging source to show non-trivial delete translation.",
                """
                DELETE u
                FROM dbo.Users u
                INNER JOIN (
                    SELECT lf.UserId
                    FROM dbo.LoginFailures lf
                    WHERE lf.AttemptedAt < @cutoffDate
                    GROUP BY lf.UserId
                ) stale ON stale.UserId = u.UserId
                WHERE u.IsLocked = 1;
                """),
            new ShowcaseSample(
                "insert-select-audit",
                "9. INSERT SELECT Audit Trail",
                "INSERT INTO ... SELECT with system functions and list parameters.",
                """
                INSERT INTO dbo.UserAudit (UserId, AuditType, CreatedAt)
                SELECT u.UserId, 'USER_EXPORT', GETUTCDATE()
                FROM dbo.Users u
                WHERE u.IsActive = 1
                  AND u.UserId IN(@userIds);
                """),
            new ShowcaseSample(
                "insert-snapshot-cte",
                "10. INSERT From Queue Snapshot",
                "CTE-driven INSERT with ROW_NUMBER partitioning and a timestamp snapshot.",
                """
                WITH backlog AS (
                    SELECT t.TicketId, t.AssignedUserId,
                           ROW_NUMBER() OVER(PARTITION BY t.AssignedUserId ORDER BY t.CreatedAt) AS QueuePosition
                    FROM dbo.Tickets t
                    WHERE t.Status = 'OPEN'
                )
                INSERT INTO dbo.TicketSnapshots (TicketId, AssignedUserId, QueuePosition, SnapshotAt)
                SELECT b.TicketId, b.AssignedUserId, b.QueuePosition, GETUTCDATE()
                FROM backlog b
                WHERE b.QueuePosition <= 10;
                """),
            new ShowcaseSample(
                "merge-select-source",
                "11. MERGE With Aggregated SELECT Source",
                "MERGE against a grouped SELECT source, demonstrating the polyfill-friendly path.",
                """
                MERGE dbo.UserScores AS trg
                USING (
                    SELECT u.UserId, SUM(s.Points) AS TotalPoints, MAX(s.UpdatedAt) AS LastUpdatedAt
                    FROM dbo.Users u
                    INNER JOIN dbo.ScoreEvents s ON s.UserId = u.UserId
                    WHERE u.UserId IN(@userIds)
                    GROUP BY u.UserId
                ) AS src
                ON trg.UserId = src.UserId
                WHEN MATCHED THEN
                    UPDATE SET trg.TotalPoints = src.TotalPoints, trg.LastUpdatedAt = src.LastUpdatedAt
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (UserId, TotalPoints, LastUpdatedAt) VALUES (src.UserId, src.TotalPoints, src.LastUpdatedAt)
                WHEN NOT MATCHED BY SOURCE THEN
                    DELETE;
                """),
            new ShowcaseSample(
                "merge-values-source",
                "12. MERGE With VALUES Source",
                "VALUES-driven MERGE is compact and highlights source-shape inference.",
                """
                MERGE dbo.FeatureFlags AS trg
                USING (VALUES
                    (1, 'BetaDashboard', 1),
                    (2, 'SmartSearch', 0),
                    (3, 'OpsMode', 1)
                ) AS src(FlagId, FlagName, IsEnabled)
                ON trg.FlagId = src.FlagId
                WHEN MATCHED THEN
                    UPDATE SET trg.FlagName = src.FlagName, trg.IsEnabled = src.IsEnabled
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (FlagId, FlagName, IsEnabled) VALUES (src.FlagId, src.FlagName, src.IsEnabled);
                """),
            new ShowcaseSample(
                "exists-recent-orders",
                "13. EXISTS Correlation",
                "Correlated EXISTS keeps the query readable while stressing subquery handling.",
                """
                SELECT u.UserId, u.Name
                FROM dbo.Users u
                WHERE EXISTS (
                    SELECT 1
                    FROM dbo.Orders o
                    WHERE o.UserId = u.UserId
                      AND o.OrderDate >= @fromDate
                )
                ORDER BY u.Name;
                """),
            new ShowcaseSample(
                "delete-derived-users",
                "14. DELETE Joined To Derived Orders",
                "A compact joined DELETE through a grouped subquery source.",
                """
                DELETE t
                FROM dbo.Users t
                INNER JOIN (
                    SELECT uoSource.UserId
                    FROM dbo.UserOrders uoSource
                    GROUP BY uoSource.UserId
                ) uo ON t.UserId = uo.UserId;
                """),
            new ShowcaseSample(
                "having-gross-sales",
                "15. Gross Sales Threshold",
                "Multi-join aggregation with date-range filtering, moved through a derived table instead of HAVING.",
                """
                SELECT gross.CustomerId, gross.CustomerName, gross.OrderCount, gross.GrossAmount
                FROM (
                    SELECT c.CustomerId, c.CustomerName, COUNT(o.OrderId) AS OrderCount, SUM(o.TotalAmount) AS GrossAmount
                    FROM dbo.Customers c
                    INNER JOIN dbo.Orders o ON o.CustomerId = c.CustomerId
                    LEFT JOIN dbo.Payments p ON p.OrderId = o.OrderId
                    WHERE o.OrderDate BETWEEN @fromDate AND @toDate
                    GROUP BY c.CustomerId, c.CustomerName
                ) gross
                WHERE gross.GrossAmount > @minimumGross
                ORDER BY gross.GrossAmount DESC;
                """),
            new ShowcaseSample(
                "string-search-functions",
                "16. String Search Functions",
                "LIKE, LEN, UPPER, and CHARINDEX in one query to show function mapping.",
                """
                SELECT u.UserId, u.Name, LEN(u.Name) AS NameLength, UPPER(u.Email) AS NormalizedEmail
                FROM dbo.Users u
                WHERE u.Name LIKE @namePattern
                  AND CHARINDEX(@emailFragment, u.Email) > 0;
                """),
            new ShowcaseSample(
                "datepart-rollup",
                "17. DATEPART Rollup",
                "DATEPART in a CTE projection, then grouped by the projected columns for calendar-style reporting.",
                """
                WITH order_calendar AS (
                    SELECT YEAR(o.OrderDate) AS OrderYear,
                           MONTH(o.OrderDate) AS OrderMonth
                    FROM dbo.Orders o
                )
                SELECT oc.OrderYear, oc.OrderMonth, COUNT(*) AS OrdersCount
                FROM order_calendar oc
                GROUP BY oc.OrderYear, oc.OrderMonth
                ORDER BY oc.OrderYear DESC, oc.OrderMonth DESC;
                """),
            new ShowcaseSample(
                "dense-rank-team",
                "18. Dense Rank Within Teams",
                "Window functions with PARTITION BY for per-team ranking.",
                """
                SELECT u.UserId, u.TeamId, u.Name, u.Score,
                       DENSE_RANK() OVER(PARTITION BY u.TeamId ORDER BY u.Score DESC) AS TeamRank
                FROM dbo.Users u
                WHERE u.TeamId IN(@teamIds)
                ORDER BY u.TeamId, u.Score DESC, u.Name;
                """),
            new ShowcaseSample(
                "multi-cte-balance",
                "19. Multi-CTE Balance Snapshot",
                "Layered CTEs chain raw transactions into per-account balances and anomaly flags.",
                """
                WITH tx AS (
                    SELECT t.AccountId, t.Amount, t.PostedAt
                    FROM dbo.AccountTransactions t
                    WHERE t.PostedAt >= @fromDate
                ),
                balance AS (
                    SELECT balanceSource.AccountId, SUM(balanceSource.Amount) AS Balance
                    FROM tx balanceSource
                    GROUP BY balanceSource.AccountId
                )
                SELECT a.AccountId, a.AccountName, b.Balance
                FROM dbo.Accounts a
                INNER JOIN balance b ON b.AccountId = a.AccountId
                WHERE b.Balance <> 0
                ORDER BY b.Balance DESC;
                """),
            new ShowcaseSample(
                "windowed-order-share",
                "20. Share Of Customer Revenue",
                "Aggregate-over-window calculation with grouped sales totals and overall revenue context.",
                """
                WITH revenue_by_customer AS (
                    SELECT c.CustomerId, c.CustomerName, SUM(o.TotalAmount) AS Revenue
                    FROM dbo.Customers c
                    INNER JOIN dbo.Orders o ON o.CustomerId = c.CustomerId
                    WHERE o.OrderDate >= @fromDate
                    GROUP BY c.CustomerId, c.CustomerName
                )
                SELECT r.CustomerId, r.CustomerName, r.Revenue,
                       SUM(r.Revenue) OVER() AS TotalRevenue,
                       SUM(r.Revenue) OVER() - r.Revenue AS RemainingRevenue
                FROM revenue_by_customer r
                ORDER BY r.Revenue DESC;
                """)
        };
    }

    internal sealed class ShowcaseSample
    {
        public ShowcaseSample(string id, string title, string description, string sqlText)
        {
            this.Id = id;
            this.Title = title;
            this.Description = description;
            this.SqlText = sqlText;
        }

        public string Id { get; }

        public string Title { get; }

        public string Description { get; }

        public string SqlText { get; }
    }
}
