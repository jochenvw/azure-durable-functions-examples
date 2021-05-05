-- Taken from: https://docs.microsoft.com/en-us/azure/app-service/app-service-web-tutorial-connect-msi

DROP USER IF EXISTS [filetrnsfrm-func-dev]
GO
CREATE USER [filetrnsfrm-func-dev] FROM EXTERNAL PROVIDER;
GO
ALTER ROLE db_datareader ADD MEMBER [filetrnsfrm-func-dev];
ALTER ROLE db_datawriter ADD MEMBER [filetrnsfrm-func-dev];
GRANT EXECUTE TO [filetrnsfrm-func-dev]
GO
