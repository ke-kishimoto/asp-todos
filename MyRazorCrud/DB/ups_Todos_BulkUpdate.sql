
CREATE OR ALTER PROCEDURE dbo.ups_Todos_BulkUpdate
AS
BEGIN
	BEGIN TRY
		BEGIN TRAN;
			UPDATE dbo.Todos set Done = 'True';
			COMMIT;

	END TRY
	BEGIN CATCH
		ROLLBACK;
		THROW;
	END CATCH
END
GO