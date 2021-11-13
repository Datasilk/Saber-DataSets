ALTER PROCEDURE [dbo].[DataSet_GetRecords]
	@datasetId int,
	@userId int = 0,
	@start int = 1,
	@length int = 50,
	@lang nvarchar(MAX) = '',
	@filters XML = '',
	/* example:	
		<groups>
			<group type="and">
				<groups>
					<group type="or">
						<element column="username" match="2" value="polymath"></element>
					</group>
				</groups>
				<elements>
					<element column="username" match="2" value="polymath"></element>
				</elements>
			</group>
		</groups>
	*/
	@sort XML = ''
	/* example:	
		<orderby>
			<sort column="userid" by="0"></sort>
			<sort column="datecreated" by="1"></sort>
		</orderby>
	*/
AS
	SET NOCOUNT ON
	DECLARE @tableName nvarchar(64)
	SELECT @tableName=tableName FROM DataSets WHERE datasetId=@datasetId
	IF @lang = '' SET @lang = 'en'

	DECLARE @sql nvarchar(MAX) = 'SELECT u.name AS username, u.email AS useremail, d.* ' + 
		'FROM DataSet_' + @tableName + ' d ' + 
		'LEFT JOIN Users u ON u.userId=d.userId ' +
		'WHERE' +
		(CASE WHEN @userId > 0 THEN ' d.userId=' + CONVERT(nvarchar(16), @userId) + ' AND' ELSE '' END) + ' d.lang=''' + @lang + ''''
	
	IF @filters IS NOT NULL BEGIN
		--get table columns
		SELECT c.[name] AS col
		INTO #cols 
		FROM sys.columns c
		INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
		WHERE c.object_id = OBJECT_ID('DataSet_' + @tableName)
		AND t.[Name] LIKE '%varchar%'
		AND c.[name] NOT IN ('lang', 'userId')

		/*
		SET @sql += ' AND ('

		DECLARE @cursor1 CURSOR, @column nvarchar(32)
		SET @cursor1 = CURSOR FOR 
		SELECT col FROM #cols
		OPEN @cursor1
		FETCH FROM @cursor1 INTO @column
		WHILE @@FETCH_STATUS = 0 BEGIN
			SET @sql += 'd.[' + @column + '] ' + 
				CASE WHEN @searchtype >= 0 THEN 'LIKE ''' ELSE ' = ''' + @search + '''' END +
				CASE WHEN @searchtype = 0 THEN '%' + @search + '%' ELSE '' END +
				CASE WHEN @searchtype = 1 THEN @search + '%' ELSE '' END +
				CASE WHEN @searchtype = 2 THEN '%' + @search ELSE '' END +
				CASE WHEN @searchtype >= 0 THEN '''' ELSE '' END
			FETCH FROM @cursor1 INTO @column
			IF @@FETCH_STATUS = 0 BEGIN
				SET @sql = @sql + ' OR '
			END
		END
		CLOSE @cursor1
		DEALLOCATE @cursor1
		SET @sql += ')'
		*/
	END

	--PRINT @sql

	--execute generated SQL code
	EXECUTE sp_executesql @sql
	
