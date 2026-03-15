# Installer for Engrafo Data Catalog Solution

**This repository holds a installation wizard for Engrafo Data Catalog Solution**

To install Engrafo Data Catalog Solution we have created an installer that has two 
installation options:

 1. Run Engrafo Data Catalog Solution in Docker 
 2. Set up a website,  database  and  the Engrafo Analyser Application on premises

Pull the project and make sure you run the project in administrator mode.
E.g. if you are using Visual Studion, then start Visial Studio in Administrator mode.

Option 1) requires that you have Docker Desktop Running. Can be run on any platform.

Option 2) requires a running Internet Information Server(IIS) and a MS SQL Server. (If you want to use the AI models in Engrafo, you need to use a 2025 MSSQL Server) 

If you need a binary installation version contact@engrafo.eu.

***What is Engrafo Data Catalog Solution...?***

> Engrafo Data Catalog Solution a data catalog tool that automatically
> creates a full data catalog, program documentation and data lineage
> for your entire data landscape using both syntax parsing and GenAI
> 
>**Features in Engrafo Data Catalog Solution:**
>- Centralized Metadata Repository for storing documentation of databases, tables, columns, and business definitions
>- Templates for Structured Data Documentation to standardize descriptions of datasets, tables, and fields
>- Automated Lineage Discovery showing how data flows between sources, transformations, and outputs
>- Impact Analysis to identify downstream systems affected by changes in data structures
>- Reverse Impact Lineage to trace a report or dataset back to its original sources
>- SQL Parser and Analyzer to automatically extract lineage and dependencies from SQL code (add-on module)
>- SAS Program Analyzer for automated documentation of SAS programs, macros, and data flows (add-on module)
>- Automated SAS Log Parsing to capture runtime metadata and execution information(add-on module)
>- table-Level Lineage to trace transformations at the field level
>- Business Glossary Management to maintain consistent business definitions across the organization
>- Data Catalog Search enabling users to easily find datasets, tables, and metadata
>- AI-Generated Documentation using generative AI to automatically create descriptions of data assets
>- AI Content Enrichment to generate summaries, pseudocode explanations, and functional descriptions of programs
>- AI Chat Interface ("Ask Engrafo") allowing users to query metadata and documentation using natural language
>- Automated Metadata Extraction from databases, code repositories, and analytics tools
>- Versioning of Documentation to track changes in metadata over time
>- Data Governance Support with structured ownership, stewardship, and classification fields
>- Role-Based Access Control for administrators, creators, and viewers
>- Active Directory / Entra ID Synchronization for user authentication and group management
>- Impact Visualization Diagrams for understanding dependencies between systems and data assets
>- Integration with BI Tools such as Power BI and Tableau for reporting on data quality, documentation progress, etc.
>- SQL API Access for programmatic interaction with the catalog
>- Crontab scheduling for impoirt of metdatadata
>- Connectionsstring to MS SQL, PostgreSQL, DB2, ORACLE, MySQL
>- Custom Metadata Fields allowing organizations to extend the catalog schema
>- Export and rule buiklder of Metadata to standard formats for governance frameworks (e.g., EHDS/DCAT or other metadata standards)
>- Export rule builder to target other systems like PurView, Colibra, Alation
>- Interactive Lineage Graphs for visual exploration of dependencies
>- Data Asset Ownership and Stewardship Tracking
>- Collaboration Features such as comments and shared documentation updates
>- Scalable Web-Based Architecture built for enterprise environments

https://www.engrafo.eu/

## SQL Server Licensing Notice

If you install Engrafo for Docker, Engrafo uses Microsoft SQL Server through an official Docker image.
SQL Server is licensed by Microsoft
and is **not** open source. By using this application, you acknowledge that
you are responsible for complying with Microsoft's licensing terms for
SQL Server.

Depending on your use case, Microsoft provides different SQL Server editions:

- **SQL Server Developer Edition** – Free, but only for development and testing.
- **SQL Server Express** – Free to use, but with feature and resource limitations.
- **SQL Server Standard/Enterprise** – Requires a paid license for production use.

Using Docker the installer does **not** distribute SQL Server itself. The database image is
downloaded directly from Microsoft’s container registry, and all usage is
governed by Microsoft's End User License Agreement (EULA).

For details, please refer to Microsoft's official SQL Server licensing terms.




