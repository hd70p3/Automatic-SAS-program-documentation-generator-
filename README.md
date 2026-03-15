# Installer for Engrafo SAS Analyzer

**This repository holds a installation wizard for Engrafo SAS Analyzer**

To install Engrafo SAS Analyzer we have created an installer that has two 
installation options:

 1. Run Engrafo SAS Analyzer in Docker 
 2. Set up a website,  database  and  the Engrafo Analyser Application on premises

Pull the project and make sure you run the project in administrator mode.
E.g. if you are using Visual Studion, then start Visial Studio in Administrator mode

Option 1) requires that you have Docker Desktop Running. Can be run on any platform.

Option 2) requires a running Internet Information Server(IIS) and a MS SQL Server. (If you want to use the AI models in Engrafo, you need to use a 2025 MSSQL Server) 

If you need a binary installation version contact@engrafo.eu

***What is Engrafo SAS Analyzer***

> Engrafo SAS analyzer module is a data catalog tool that automatically
> creates a full data catalog, program documentation and data lineage
> for your entire SAS landscape using both syntax parsing and GenAI
> 
> SAS is an incredibly flexible ETL and analytics tool, offering users
> numerous ways to achieve their desired results. With Engrafo SAS
> Analyzer, you can enhance governance and compliance by allowing users
> to  commit their SAS program logs to Engrafo. This provides both users
> and the organization with a comprehensive overview of data usage,
> data lineage across different domains, and a powerful tool for
> searching data definitions, data dictionaries, view detailed flow diagrams of the code.
>
> Engrafo SAS Analyzer ensures you have the insights and control needed to optimize
> your SAS environment.
>
> Out of the box yout get:
>- a full data catalog application with all referenced data and documentation of your SAS programs
>- detailed flow charts of all SAS programs
>- overview over SAS procedures used
>- list of both logical and physical libnames 
>- input and output data and external files for all programs
>- all metadata stored in a SQL data model to be utilized in e.g. PowerBI, Tableau or direct SQL queries
>- data lineage visualization, impact and reverse lineage on libraries, tables and columns 
>- AI models to get deep insights in your SAS programs
>  
> Get answers on questions like:  
>- What is the impact for changing selected libraries, tables or columns?
>- Which programs are depended on each other?
>- What procedures do we use? 
>- What are the usages freqensies?  
>- Do we have the right SAS licenses?  
>- Which programs, procedures, data steps require the most time to execute? 
>- Where are the bottlenecks in the SAS programs
>- Where does external files come from and how is the lineage for them??

https://www.engrafo.eu/automated-sas-data-catalog-and-lineage/

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




