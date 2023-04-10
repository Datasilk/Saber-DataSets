DROP PROCEDURE IF EXISTS [dbo].[DataSets_Relationship_Create]
GO
CREATE PROCEDURE [dbo].[DataSets_Relationship_Create]
	@parentId INT,
	@childId INT,
	@childColumn nvarchar(32),
	@parentList nvarchar(32),
	@listtype INT
AS
	--first remove childColumn if neccessary
	IF @listtype <> 2 SET @childColumn = ''

	IF EXISTS(SELECT * FROM Datasets_Relationships WHERE parentId=@parentId AND childId=@childId) BEGIN
		DELETE FROM Datasets_Relationships WHERE parentId=@parentId AND childId=@childId
	END

	INSERT INTO Datasets_Relationships (parentId, childId, parentList, childcolumn, listtype)
	VALUES (@parentId, @childId, @parentList, @childColumn, @listtype)