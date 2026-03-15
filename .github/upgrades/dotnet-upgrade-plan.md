# .NET 10.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 10.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 10.0 upgrade.
3. Upgrade Core.Business.Objects\Core.Business.Objects.csproj
4. Upgrade NumberArtist.Api\NumberArtist.Api.csproj
5. Upgrade NumberArtistView\NumberArtistView.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name                                         | Current Version | New Version | Description                                   |
|:-----------------------------------------------------|:---------------:|:-----------:|:----------------------------------------------|
| Microsoft.AspNetCore.Authentication.JwtBearer        | 9.0.12          | 10.0.5      | Recommended for .NET 10.0                     |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore    | 9.0.12          | 10.0.5      | Recommended for .NET 10.0                     |
| Microsoft.AspNetCore.OpenApi                         | 9.0.12          | 10.0.5      | Recommended for .NET 10.0                     |
| Microsoft.EntityFrameworkCore.Sqlite.Core            | 9.0.10          | 10.0.5      | Recommended for .NET 10.0                     |
| Microsoft.EntityFrameworkCore.SqlServer              | 9.0.12          | 10.0.5      | Recommended for .NET 10.0                     |
| Microsoft.EntityFrameworkCore.Tools                  | 9.0.12          | 10.0.5      | Recommended for .NET 10.0                     |
| Microsoft.Extensions.Logging.Debug                   | 9.0.10          | 10.0.5      | Recommended for .NET 10.0                     |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### Core.Business.Objects\Core.Business.Objects.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore should be updated from `9.0.12` to `10.0.5` (*recommended for .NET 10.0*)

#### NumberArtist.Api\NumberArtist.Api.csproj modifications

Project properties changes:
  - Target framework should be changed from `net9.0` to `net10.0`

NuGet packages changes:
  - Microsoft.AspNetCore.Authentication.JwtBearer should be updated from `9.0.12` to `10.0.5` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.Identity.EntityFrameworkCore should be updated from `9.0.12` to `10.0.5` (*recommended for .NET 10.0*)
  - Microsoft.AspNetCore.OpenApi should be updated from `9.0.12` to `10.0.5` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Sqlite.Core should be updated from `9.0.10` to `10.0.5` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.SqlServer should be updated from `9.0.12` to `10.0.5` (*recommended for .NET 10.0*)
  - Microsoft.EntityFrameworkCore.Tools should be updated from `9.0.12` to `10.0.5` (*recommended for .NET 10.0*)

#### NumberArtistView\NumberArtistView.csproj modifications

Project properties changes:
  - Target frameworks should be changed from `net9.0-android;net9.0-maccatalyst;net9.0-windows10.0.19041.0` to `net9.0-android;net9.0-maccatalyst;net9.0-windows10.0.19041.0;net10.0-windows`

NuGet packages changes:
  - Microsoft.Extensions.Logging.Debug should be updated from `9.0.10` to `10.0.5` (*recommended for .NET 10.0*)
