USE FCT_OCT_1
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DEL PROCEDIMIENTO:	PA_ACTUALIZAR_MOVIMIENTOS
	FECHA DE CREACIÓN: 		25/11/2025
	AUTOR:				Rubén
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\PROCEDIMIENTOS ALMACENADOS
	USO:				##VISUAL##

	FUNCIONAMIENTO:			ACTUALIZA LA TABLA MOVIMIENTOS A REALIZADO Y A UBICACIONES CON LOS NUEVOS PALETS GENERADOS

	PARAMETROS:			(OPCIONAL)
		PARAMETRO1 		INPUT	EXPLICACION
		PARAMETRO2 		OUTPUT	EXPLICACION

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

CREATE PROCEDURE PA_ACTUALIZAR_MOVIMIENTOS
	
	@ID_MOVIMIENTOS INT,

	@INVOKER	INT,		-- ESTE PARÁMETRO LO DEBEN TENER TODOS LOS PAS
	@USUARIO	VARCHAR(12),	-- ESTE PARÁMETRO LO DEBEN TENER TODOS LOS PAS
	@CULTURA	VARCHAR(5),

	@RETCODE	INT OUTPUT, --DEFINICIÓN OBLIGATORIA
	@MENSAJE	VARCHAR(1000)	OUTPUT	--DEFINICIÓN OBLIGATORIA

AS

BEGIN TRY
	--DECLARACION DE VARIABLES 

	DECLARE @N_TRANS		INT = 0	 --NUMERO DE TRANSACCIONES ACTIVAS	(@@TRANCOUNT)
	SET @N_TRANS = @@TRANCOUNT

	DECLARE @PETICION INT
	DECLARE @PALET	INT
	DECLARE @CANTIDAD INT
	DECLARE @REFERENCIA VARCHAR(30)
	DECLARE @UBICACION_ORIGEN VARCHAR(30)
	DECLARE @UBICACION_DESTINO VARCHAR(30)
	DECLARE @LIN_PETICION INT
	DECLARE @REALIZADO INT




	--COMPROBACIONES
	 IF NOT EXISTS (SELECT 1 FROM Movimientos WHERE ID_MOVIMIENTOS = @ID_MOVIMIENTOS)
	BEGIN
		SET @MENSAJE = 'El movimiento con ID ' + CAST(@ID_MOVIMIENTOS AS VARCHAR) + ' no existe.';
		SET @RETCODE = 1
	END
	
	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		BEGIN TRANSACTION TR_NOMBRE_TRANSACTION
	END
	----------------------------------------------------------------------------------------------------------------------------------------------

	--OPERACIONES
	UPDATE 
		MOVIMIENTOS
	SET 
		REALIZADO = 1,
		UBICACION_DESTINO = 'TRANSPORTE'
	WHERE
		ID_MOVIMIENTOS = @ID_MOVIMIENTOS
	

	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		COMMIT TRANSACTION TR_NOMBRE_TRANSACTION
	END
	----------------------------------------------------------------------------------------------------------------------------------------------


	SET @MENSAJE = 'El proceso se ha realizado correctamente.'
	SET @RETCODE = 0
	RETURN @RETCODE
END TRY
BEGIN CATCH
	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @N_TRANS = 0 AND @@TRANCOUNT > 0				-- Si hay una transacción por encima no hacemos nada
	BEGIN
		ROLLBACK TRANSACTION TR_NOMBRE_TRANSACTION
	END
	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @MENSAJE = '' 
	BEGIN
		SET  @MENSAJE = ERROR_MESSAGE()
	END
	
	SET @RETCODE = -1
		
	RETURN @RETCODE
END CATCH

	SET @RETCODE = -1		
	RETURN @RETCODE






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
