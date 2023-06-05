IF  NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DataLog]') AND type in (N'U')) 
BEGIN
    create table DataLog(
		id int primary key identity(1,1),
		Log_Code varchar(200) null, 
		Log_Message varchar(max) null, 
		Log_Type varchar(100) null, 
		Log_Date datetime null,
	)
END