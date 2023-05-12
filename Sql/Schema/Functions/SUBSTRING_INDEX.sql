DROP FUNCTION IF EXISTS [dbo].[SUBSTRING_INDEX]
GO
CREATE FUNCTION [dbo].[SUBSTRING_INDEX]
(
   @ExistingString NVARCHAR(MAX),
   @BreakPoint NVARCHAR(MAX),
   @number INT
)
RETURNS NVARCHAR(MAX)
AS
BEGIN
    DECLARE @Count INT
    DECLARE @SubstringLength INT
    DECLARE @Substring NVARCHAR(MAX)
    DECLARE @ssubstring NVARCHAR(MAX)
    SET @ssubstring=@ExistingString
    DECLARE @scount INT
    SET @scount=0
    DECLARE @sscount INT
    SET @sscount=0
    DECLARE @number2 INT
    DECLARE @occurence INT
    SET @occurence=LEN(@ExistingString) - LEN(REPLACE(@ExistingString, @BreakPoint, ''))
    If @number<0
         BEGIN
            SET @number2= @occurence-(-1*@number)+1
         END
    If @number>0
         BEGIN
            SET @number2=@number
         END
    WHILE(@number2>@scount)
        BEGIN
                Select @Count=CHARINDEX(@BreakPoint,@ExistingString)
                Select @SubstringLength=@Count+LEN(@BreakPoint) 
                Select @ExistingString=SUBSTRING(@ExistingString,@SubstringLength,LEN(@ExistingString)-@Count)
                Select @scount=@scount+1 
                select @sscount=@sscount+@Count
        END
    If @number<0
         BEGIN
            if (@number = -1) and (@sscount+LEN(@BreakPoint)) = (LEN(@ssubstring)+1)
                BEGIN
                   SELECT @Substring=''
                END
            else if @occurence = 0
                BEGIN
                   SELECT @Substring=''
                END
            else
                BEGIN
                   SELECT @Substring=SUBSTRING(@ssubstring, @sscount+LEN(@BreakPoint), LEN(@ssubstring))
                END
         END
    If @number>0
        if @occurence = 0
                BEGIN
                   SELECT @Substring=''
                END
            else
                BEGIN
                   SELECT @Substring=SUBSTRING(@ssubstring,0,@sscount)
                END

    RETURN @Substring
END