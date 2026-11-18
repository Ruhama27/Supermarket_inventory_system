# 🚀 Stock-Sync Deployment & Installation Guide

This guide covers how to deploy and run **Stock-Sync** in production, ranging from a single all-in-one computer to a multi-terminal supermarket network.

---

## 📋 Prerequisites

| Component | Minimum Requirement | Notes |
| :--- | :--- | :--- |
| **Operating System** | Windows 10 or Windows 11 (64-bit / 32-bit) | Also supports Windows Server 2016+ |
| **.NET Runtime** | .NET Framework 4.7.2 or higher | Built-in by default on Windows 10 & 11 |
| **Database Server** | Microsoft SQL Server (Express, Standard, or Enterprise) | 2014 or newer (`.\SQLEXPRESS` recommended) |
| **Hardware** | 2 GB RAM, 500 MB Free Disk Space | Thermal receipt printer & USB barcode scanner optional |

---

## 🗄️ Step 1: Database Setup (One-Time Server Setup)

The database only needs to be installed once on the server or host computer.

1. Install **Microsoft SQL Server Express** and **SQL Server Management Studio (SSMS)**.
2. Open SSMS and connect to your SQL Server instance (e.g. `.\SQLEXPRESS`).
3. Open and execute [`setup_database.sql`](setup_database.sql):
   - Creates the `InventoryDB` database.
   - Creates tables, audit triggers, and stored procedures.
   - Inserts initial credentials:
     - **Admin**: Username: `admin` | Password: `admin123`
     - **Staff**: Username: `staff` | Password: `staff123`
4. (Optional) If updating an existing database, run [`upgrade_database.sql`](upgrade_database.sql).

---

## 📦 Step 2: Choose Your Deployment Method

### Method A: Portable Distribution (Fastest & Simplest)

Best for running directly from a folder, USB drive, or shared network drive without installing software.

1. Run the included automated packaging script in PowerShell:
   ```powershell
   .\package_release.ps1
   ```
2. This creates:
   - `dist\StockSync_Portable_v1.0\` (Ready-to-run folder)
   - `dist\StockSync_Portable_v1.0.zip` (Portable ZIP package)
3. Copy the folder to any target Windows computer and double-click `Start-StockSync.bat` (or `Stock_Sync.exe`).

---

### Method B: Professional Windows Setup Wizard (`Setup.exe`)

Best for giving store managers a standard Windows installation experience with a Desktop icon, Start Menu shortcut, and Windows Control Panel uninstaller.

1. Download and install the free, open-source [Inno Setup Compiler](https://jrsoftware.org/isdl.php).
2. Right-click [`installer_script.iss`](installer_script.iss) and select **Compile**.
3. Inno Setup will compile a single file installer in the `dist/` directory:
   ```
   dist\StockSync_Setup_v1.0.exe
   ```
4. Distribute `StockSync_Setup_v1.0.exe` to cashiers or store managers to install with a standard next-next-finish wizard.

---

### Method C: Multi-Terminal Supermarket Setup (LAN Client-Server)

In a typical supermarket, you have **one Central Database Server** (in the back office or on the manager's PC) and **multiple POS Cashier Terminals** at the checkout counters.

```
       ┌──────────────────────────────────────────────┐
       │     Central Store Server (192.168.1.100)     │
       │          Microsoft SQL Server                │
       │             [ InventoryDB ]                  │
       └──────────────────────┬───────────────────────┘
                              │
          ┌───────────────────┼───────────────────┐
          │ (Local Wi-Fi / Ethernet LAN)          │
          ▼                                       ▼
┌──────────────────┐                    ┌──────────────────┐
│  POS Terminal 1  │                    │  POS Terminal 2  │
│  Stock_Sync.exe  │                    │  Stock_Sync.exe  │
└──────────────────┘                    └──────────────────┘
```

#### On the Database Server PC:
1. Open **SQL Server Configuration Manager**:
   - Go to **SQL Server Network Configuration** > **Protocols for SQLEXPRESS**.
   - Enable **TCP/IP**.
   - Double-click **TCP/IP**, open the **IP Addresses** tab, scroll to **IPAll**, and set **TCP Port** to `1433`.
2. Open **Windows Firewall with Advanced Security**:
   - Add an **Inbound Rule** allowing **Port 1433** (TCP) for SQL Server.
3. Find the server's local IP address (e.g., `192.168.1.100`).

#### On each Cashier POS Terminal:
1. Copy the `Stock-Sync` application folder.
2. Open `Stock_Sync.exe.config` in Notepad and update the connection string to point to the server:
   ```xml
   <connectionStrings>
       <add name="InventoryDB" 
            connectionString="Data Source=192.168.1.100\SQLEXPRESS;Initial Catalog=InventoryDB;User Id=sa;Password=YourServerPassword;TrustServerCertificate=True" 
            providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```
3. Run `Stock_Sync.exe`. All POS terminals will now synchronize transactions, stock deductions, and sales in real time!

---

### Method D: Cloud Database Deployment (Azure SQL / AWS RDS)

If you manage multiple store branches or want remote access:
1. Create a cloud database (e.g. **Azure SQL Database** or **AWS RDS for SQL Server**).
2. Run `setup_database.sql` against the cloud database.
3. Update `Stock_Sync.exe.config` on all client machines with your cloud connection string:
   ```xml
   <connectionStrings>
       <add name="InventoryDB" 
            connectionString="Server=tcp:yourserver.database.windows.net,1433;Initial Catalog=InventoryDB;Persist Security Info=False;User ID=cloudadmin;Password=YourCloudPassword;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" 
            providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```

---

## 🛠️ Post-Deployment Verification Checklist

- [ ] Can connect and log in as `admin` (Password: `admin123`).
- [ ] Language switcher toggles cleanly between **English** and **አማርኛ**.
- [ ] Test barcode scanner or manual entry on the **Point of Sale (POS)** screen.
- [ ] Verify thermal receipt printing with your connected receipt printer.
- [ ] Check `logs/stock_sync.log` to confirm zero startup warnings or connection errors.
