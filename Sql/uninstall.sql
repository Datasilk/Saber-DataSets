DROP TABLE DataSets
DROP TABLE DataSets_Relationships

DROP SEQUENCE SequenceDataSets

DROP PROCEDURE DataSet_AddRecord
DROP PROCEDURE DataSet_Create
DROP PROCEDURE DataSet_Delete
DROP PROCEDURE DataSet_DeleteRecord
DROP PROCEDURE DataSet_GetAllColumns
DROP PROCEDURE DataSet_GetColumns
DROP PROCEDURE DataSet_GetInfo
DROP PROCEDURE DataSet_UpdateColumns
DROP PROCEDURE DataSet_UpdateInfo
DROP PROCEDURE DataSet_UpdateRecord
DROP PROCEDURE DataSets_GetList
DROP PROCEDURE DataSets_Relationship_Create
DROP PROCEDURE DataSets_Relationships_GetAll
DROP PROCEDURE DataSets_Relationships_GetList

--remove all user-created datasets
DECLARE @sql nvarchar(MAX)
SELECT @sql=STRING_AGG('DROP TABLE ' + TABLE_NAME, char(10)) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE 'DataSet_%'
EXECUTE sp_executesql @sql

--remove all user-created sequences
SELECT @sql=STRING_AGG('DROP SEQUENCE ' + SEQUENCE_NAME, char(10)) FROM INFORMATION_SCHEMA.SEQUENCES WHERE SEQUENCE_NAME LIKE 'Sequence_DataSet_%'
EXECUTE sp_executesql @sql