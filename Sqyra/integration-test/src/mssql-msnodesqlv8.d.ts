declare module "mssql/msnodesqlv8.js" {
  import type * as Mssql from "mssql";
  const driver: typeof Mssql;
  export default driver;
}
