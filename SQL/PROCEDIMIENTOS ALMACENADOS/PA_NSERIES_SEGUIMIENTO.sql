USE FCT_OCT_1
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DEL PROCEDIMIENTO:	PA_NSERIES_SEGUIMIENTO
	FECHA DE CREACIÓN: 		20/11/2025
	AUTOR:				Rubén
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\PROCEDIMIENTOS ALMACENADOS
	USO:				##VISUAL##

	FUNCIONAMIENTO:			AL HACER EL PICKING Y COMPLETARLO, GUARDA LOS PRODUCTOS CON NSERIE SOLICITADOS

	PARAMETROS:			(OPCIONAL)
		PARAMETRO1 		INPUT	EXPLICACION
		PARAMETRO2 		OUTPUT	EXPLICACION

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

ALTER PROCEDURE [dbo].[PA_NSERIES_SEGUIMIENTO]
	
	@NSERIE		VARCHAR(30),
	@PALET		INT,
	@ALBARAN	INT,
	@REFERENCIA	VARCHAR(30),
	

	@INVOKER	INT,		-- ESTE PARÁMETRO LO DEBEN TENER TODOS LOS PAS
	@USUARIO	VARCHAR(12),	-- ESTE PARÁMETRO LO DEBEN TENER TODOS LOS PAS
	@CULTURA	VARCHAR(5),

	@RETCODE	INT OUTPUT, --DEFINICIÓN OBLIGATORIA
	@MENSAJE	VARCHAR(1000)	OUTPUT	--DEFINICIÓN OBLIGATORIA

AS

BEGIN TRY
	--DECLARACION DE VARIABLES 
	DECLARE @ALBARAN_RECEPCION INT;

	DECLARE @N_TRANS		INT = 0	 --NUMERO DE TRANSACCIONES ACTIVAS	(@@TRANCOUNT)
	SET @N_TRANS = @@TRANCOUNT

	--COMPROBACIONES
	----------------------------------------------------------------------------------------------------------------------------------------------
	/*IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		BEGIN TRANSACTION TR_NOMBRE_TRANSACTION
	END*/
	----------------------------------------------------------------------------------------------------------------------------------------------

	--OPERACIONES
	SELECT TOP 1 @ALBARAN_RECEPCION = ALBARAN
	FROM NSeries_Recepciones
	WHERE NSerie = @NSERIE AND Referencia = @REFERENCIA;

	IF @ALBARAN_RECEPCION IS NULL
	BEGIN
		SET @MENSAJE = 'No se encontró el albarán de recepción para este número de serie.';
		SET @RETCODE = -1;
		RETURN;
	END

	INSERT INTO NSERIES_SEGUIMIENTO(NSERIE, PALET, ALBARAN, REFERENCIA, F_PICKING)
	VALUES (@NSERIE, @PALET, @ALBARAN_RECEPCION, @REFERENCIA, GETDATE());


	----------------------------------------------------------------------------------------------------------------------------------------------
	/*IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		COMMIT TRANSACTION TR_NOMBRE_TRANSACTION
	END*/
	----------------------------------------------------------------------------------------------------------------------------------------------


	SET @MENSAJE = 'El proceso se ha realizado correctamente.'
	SET @RETCODE = 0
	

END TRY
BEGIN CATCH
	----------------------------------------------------------------------------------------------------------------------------------------------
	/*IF @N_TRANS = 0 AND @@TRANCOUNT > 0				-- Si hay una transacción por encima no hacemos nada
	BEGIN
		ROLLBACK TRANSACTION TR_NOMBRE_TRANSACTION
	END*/
	----------------------------------------------------------------------------------------------------------------------------------------------
		SET  @MENSAJE = ISNULL(ERROR_MESSAGE(), 'Error desconocido en PA_NSERIES_SEGUIMIENTO');
		SET @RETCODE = -1

END CATCH






/*----------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------               PRUEBAS              ------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------


DECLARE @RETCODE	INT
DECLARE @PARAMETRO1	VARCHAR(10)
DECLARE @PARAMETRO2	INT
DECLARE @PARAMETRO3	VARCHAR(20)
DECLARE @MENSAJE	VARCHAR(1000)

SET @MENSAJE 		= ''

EXEC @RETCODE = PA_XXXX @PARAMETRO1, @PARAMETRO2, @PARAMETRO3 OUTPUT, @MENSAJE OUTPUT

PRINT 'RETCODE:	' 	+ ISNULL(CAST(@RETCODE AS VARCHAR(10)), 'NULO')
PRINT 'MENSAJE:	' 	+ ISNULL(@MENSAJE, 'NULO')
PRINT 'PARAMETRO3''	+ ISNULL(@PARAMETRO3, 'NULO')



------------------------------------------------------------------------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
------------------------------             FIN PRUEBAS            ------------------------------------------------------
------------------------------------------------------------------------------------------------------------------------
----------------------------------------------------------------------------------------------------------------------*/
