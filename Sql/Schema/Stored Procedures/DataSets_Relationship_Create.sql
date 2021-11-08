CREATE PROCEDURE [dbo].[DataSets_Relationship_Create]
	@parentId INT,
	@childId INT,
	@parentList nvarchar(32),
	@childColumn nvarchar(32)
AS
	IF EXISTS(SELECT * FROM Datasets_Relationships WHERE parentId=@parentId AND childId=@childId) BEGIN
		DELETE FROM Datasets_Relationships WHERE parentId=@parentId AND childId=@childId
	END

	INSERT INTO Datasets_Relationships (parentId, childId, parentList, childColumn)
	VALUES (@parentId, @childId, @parentList, @childColumn)