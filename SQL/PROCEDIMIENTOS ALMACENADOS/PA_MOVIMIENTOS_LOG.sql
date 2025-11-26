USE [FCT_OCT_1]
GO
/****** Object:  StoredProcedure [dbo].[PA_MOVIMIENTOS_LOG]    Script Date: 14/11/2025 14:20:20 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

/*-----------------------------------------------------------------------------------------------------------------------------------------------------

	NOMBRE DEL PROCEDIMIENTO:	PA_MOVIMIENTOS_LOG
	FECHA DE CREACIÓN: 		14/11/2025
	AUTOR:				Rubén
	VSS:				E:\DevOps\PRACTICAS\FCT_PRACTICAS\2025\FCT_OCT_1\SQL\PROCEDIMIENTOS ALMACENADOS
	USO:				##VISUAL##

	FUNCIONAMIENTO:			ACTUALIZAR TABLA MOVIMIENTOS LOG.

	PARAMETROS:			(OPCIONAL)
		PARAMETRO1 		INPUT	EXPLICACION
		PARAMETRO2 		OUTPUT	EXPLICACION

-------------------------------------------------------------------------------------------------------------------------------------------------------
--	FECHA DE MODIFICACIÓN:
--	AUTOR:
--	EXPLICACIÓN:	
------------------------------------------------------------------------------------------------------------------------------------------------------*/

ALTER PROCEDURE [dbo].[PA_MOVIMIENTOS_LOG]
	
	@PETICION	INT,
	@PALET		INT,
	@REFERENCIA	VARCHAR(30),
	@UBICACION_ORIGEN VARCHAR(30),
	@UBICACION_DESTINO VARCHAR(30),
	@FECHA_MOVIMIENTO DATETIME,
	@IDUSUARIO	INT,
	

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

	--COMPROBACIONES
	IF NOT EXISTS (SELECT 1 FROM USUARIOS WHERE IDUSUARIO = @IDUSUARIO)
	BEGIN
		SET @MENSAJE = 'El usuario con ID ' + CAST(@IDUSUARIO AS VARCHAR(10)) + ' no existe.';
		SET @RETCODE = 1;
		RETURN @RETCODE;
	END
	
	----------------------------------------------------------------------------------------------------------------------------------------------
	IF @N_TRANS = 0						-- Si hay una transacción por encima no hacemos nada
	BEGIN
		BEGIN TRANSACTION TR_NOMBRE_TRANSACTION
	END
	----------------------------------------------------------------------------------------------------------------------------------------------

	--OPERACIONES
	INSERT INTO MOVIMIENTOS_LOG(PETICION, PALET, REFERENCIA, UBICACION_ORIGEN,  UBICACION_DESTINO, FECHA_MOVIMIENTO, IDUSUARIO)
	VALUES(@PETICION, @PALET, @REFERENCIA, @UBICACION_ORIGEN, @UBICACION_DESTINO, @FECHA_MOVIMIENTO, @IDUSUARIO)


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
