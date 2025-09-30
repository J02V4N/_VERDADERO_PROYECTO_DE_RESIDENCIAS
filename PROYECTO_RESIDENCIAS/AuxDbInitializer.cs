using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PROYECTO_RESIDENCIAS
{
    public static class AuxDbInitializer
    {
        public static string GetDefaultAuxDbPath()
        {
            // 1) Intenta ubicar la raíz del proyecto (carpeta que contiene el .csproj).
            //    Esto funciona cuando ejecutas desde Visual Studio (Debug/Run).
            string appRoot = ResolveAppRoot();

            // 2) Si no se encontró (p. ej., ejecutable publicado), usa el folder del .exe.
            if (string.IsNullOrWhiteSpace(appRoot))
                appRoot = System.AppContext.BaseDirectory;

            // 3) Construye la ruta final (en raíz del proyecto o junto al exe).
            string dbPath = Path.Combine(appRoot, "RESTAURANTE.FDB");

            // Asegura que el directorio existe (root del proyecto o del exe ya existe, pero por si acaso):
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            return dbPath;
        }

        // Busca hacia arriba (máx. 5 niveles) hasta encontrar un *.csproj y toma ese folder como raíz del proyecto.
        private static string ResolveAppRoot()
        {
            string dir = System.AppContext.BaseDirectory;
            try
            {
                var d = new DirectoryInfo(dir);
                for (int i = 0; i < 5 && d != null; i++, d = d.Parent)
                {
                    bool hasCsproj = Directory.EnumerateFiles(d.FullName, "*.csproj", SearchOption.TopDirectoryOnly).Any();
                    if (hasCsproj)
                        return d.FullName;
                }
            }
            catch
            {
                // ignora y sigue con fallback
            }
            return string.Empty;
        }

        public static FbConnection EnsureCreated(
            out string dbPath,
            string server = "127.0.0.1",
            int port = 3050,
            string user = "SYSDBA",
            string password = "masterkey",
            string charset = "ISO8859_1")
        {
            dbPath = GetDefaultAuxDbPath();

            var csb = new FbConnectionStringBuilder
            {
                DataSource = server,
                Port = port,
                Database = dbPath,
                UserID = user,
                Password = password,
                Charset = charset,
                Dialect = 3,
                Pooling = true
                // Si necesitas especificar fbclient.dll x86:
                // ClientLibrary = "fbclient.dll"
            };

            if (!File.Exists(dbPath))
            {
                // Crea la BD con el charset dado como DEFAULT CHARACTER SET
                FbConnection.CreateDatabase(csb.ToString(), 8192, false);
            }

            var conn = new FbConnection(csb.ToString());
            conn.Open();

            // Si no existe la tabla USUARIOS, asumimos que no existe el esquema
            if (!TableExists(conn, "USUARIOS"))
            {
                CreateSchema(conn);
                // Semillas mínimas
                SeedConfig(conn);
            }

            return conn;
        }

        public static void UpsertConfig(FbConnection conn, string clave, string valor)
        {
            // 1) UPDATE tipado
            using (var upd = new FbCommand("UPDATE CONFIG SET VALOR=@VALOR WHERE CLAVE=@CLAVE", conn))
            {
                upd.Parameters.Add(new FbParameter("@VALOR", FbDbType.VarChar, 255) { Value = (valor ?? "") });
                upd.Parameters.Add(new FbParameter("@CLAVE", FbDbType.VarChar, 50) { Value = clave });
                int rows = upd.ExecuteNonQuery();

                if (rows > 0) return;
            }

            // 2) INSERT tipado
            using (var ins = new FbCommand("INSERT INTO CONFIG (CLAVE, VALOR) VALUES (@CLAVE, @VALOR)", conn))
            {
                ins.Parameters.Add(new FbParameter("@CLAVE", FbDbType.VarChar, 50) { Value = clave });
                ins.Parameters.Add(new FbParameter("@VALOR", FbDbType.VarChar, 255) { Value = (valor ?? "") });
                ins.ExecuteNonQuery();
            }
        }


        private static bool TableExists(FbConnection conn, string tableNameUpper)
        {
            using var cmd = new FbCommand(@"
SELECT 1
FROM RDB$RELATIONS
WHERE RDB$SYSTEM_FLAG = 0
  AND RDB$RELATION_NAME = @T;", conn);

            cmd.Parameters.Add(new FbParameter("@T", FbDbType.Char, 31) { Value = tableNameUpper.ToUpperInvariant() });
            var o = cmd.ExecuteScalar();
            return o != null && o != DBNull.Value;
        }

        private static void CreateSchema(FbConnection conn)
        {
            // Ejecutar cada sentencia por separado en una sola transacción
            using var tx = conn.BeginTransaction();

            void Exec(string sql)
            {
                using var cmd = new FbCommand(sql, conn, tx);
                cmd.ExecuteNonQuery();
            }

            // ====== SECUENCIAS (GENERATORS) ======
            Exec("CREATE SEQUENCE GEN_USUARIOS_ID;");
            Exec("CREATE SEQUENCE GEN_MESAS_ID;");
            Exec("CREATE SEQUENCE GEN_MESEROS_ID;");
            Exec("CREATE SEQUENCE GEN_TURNOS_ID;");
            Exec("CREATE SEQUENCE GEN_MESA_TURNO_ID;");
            Exec("CREATE SEQUENCE GEN_PEDIDOS_ID;");
            Exec("CREATE SEQUENCE GEN_PEDIDO_DET_ID;");
            Exec("CREATE SEQUENCE GEN_RECETAS_ID;");
            Exec("CREATE SEQUENCE GEN_RECETA_DET_ID;");
            Exec("CREATE SEQUENCE GEN_BASCULA_LECT_ID;");

            // ====== TABLAS ======

            // USUARIOS (lista para crecer a roles y multiempresa)
            Exec(@"
CREATE TABLE USUARIOS (
  ID_USUARIO      INTEGER NOT NULL PRIMARY KEY,
  USERNAME        VARCHAR(50) NOT NULL UNIQUE,
  PASSWORD_HASH   VARCHAR(128) NOT NULL,
  PASSWORD_SALT   VARCHAR(64),
  NOMBRE_COMPLETO VARCHAR(100),
  EMAIL           VARCHAR(100),
  ACTIVO          SMALLINT NOT NULL, /* 1=Sí, 0=No */
  EMPRESA_DEF     SMALLINT,          /* Empresa SAE por defecto (01..99) - opcional futuro */
  ROL_PRESET      VARCHAR(20),       /* 'ADMIN'|'MESERO'|'CAJA'... (placeholder futuro) */
  FECHA_ALTA      TIMESTAMP NOT NULL,
  ULT_LOGIN       TIMESTAMP
);");

            // MESAS
            Exec(@"
CREATE TABLE MESAS (
  ID_MESA    INTEGER NOT NULL PRIMARY KEY,
  NOMBRE     VARCHAR(30) NOT NULL,
  CAPACIDAD  SMALLINT,
  ESTADO     VARCHAR(12) NOT NULL /* LIBRE | OCUPADA | EN_CUENTA | CERRADA */
);");

            // MESEROS
            Exec(@"
CREATE TABLE MESEROS (
  ID_MESERO  INTEGER NOT NULL PRIMARY KEY,
  NOMBRE     VARCHAR(60) NOT NULL,
  ACTIVO     SMALLINT NOT NULL /* 1=Sí, 0=No */
);");

            // TURNOS
            Exec(@"
CREATE TABLE TURNOS (
  ID_TURNO      INTEGER NOT NULL PRIMARY KEY,
  FECHA         DATE NOT NULL,
  HORA_INI      TIME NOT NULL,
  HORA_FIN      TIME,
  RESPONSABLE   VARCHAR(60) NOT NULL,
  OBS           VARCHAR(255)
);");

            // MESA_TURNO (asignación de mesero a mesa por turno)
            Exec(@"
CREATE TABLE MESA_TURNO (
  ID_MESA_TURNO  INTEGER NOT NULL PRIMARY KEY,
  ID_TURNO       INTEGER NOT NULL,
  ID_MESA        INTEGER NOT NULL,
  ID_MESERO      INTEGER NOT NULL,
  ESTADO         VARCHAR(12) NOT NULL /* LIBRE | OCUPADA | EN_CUENTA | CERRADA */,
  CONSTRAINT FK_MT_TURNO  FOREIGN KEY (ID_TURNO)  REFERENCES TURNOS(ID_TURNO),
  CONSTRAINT FK_MT_MESA   FOREIGN KEY (ID_MESA)   REFERENCES MESAS(ID_MESA),
  CONSTRAINT FK_MT_MESERO FOREIGN KEY (ID_MESERO) REFERENCES MESEROS(ID_MESERO)
);");

            // PEDIDOS (encabezado)
            Exec(@"
CREATE TABLE PEDIDOS (
  ID_PEDIDO         INTEGER NOT NULL PRIMARY KEY,
  ID_MESA_TURNO     INTEGER NOT NULL,
  FECHA_HORA        TIMESTAMP NOT NULL,
  ESTADO            VARCHAR(12) NOT NULL /* ABIERTO | COBRADO | CANCELADO */,
  SUBTOTAL          DECIMAL(18,6),
  IMPUESTO          DECIMAL(18,6),
  TOTAL             DECIMAL(18,6),
  OBS               VARCHAR(255),
  FACTURAR_AHORA    SMALLINT NOT NULL, /* 1=Sí, 0=No */
  RFC               VARCHAR(13),
  RAZON_SOCIAL      VARCHAR(120),
  USO_CFDI          VARCHAR(5),
  METODO_PAGO       VARCHAR(10),
  FORMA_PAGO        VARCHAR(5),
  CLIENTE_CLAVE_SAE VARCHAR(30),
  CONSTRAINT FK_PED_MT FOREIGN KEY (ID_MESA_TURNO) REFERENCES MESA_TURNO(ID_MESA_TURNO)
);");

            // PEDIDO_DET (detalle)
            Exec(@"
CREATE TABLE PEDIDO_DET (
  ID_PEDIDO_DET      INTEGER NOT NULL PRIMARY KEY,
  ID_PEDIDO          INTEGER NOT NULL,
  CLAVE_ARTICULO_SAE VARCHAR(30) NOT NULL, /* platillo o insumo SAE */
  ES_PLATILLO        SMALLINT NOT NULL, /* 1=platillo, 0=insumo directo */
  CANTIDAD           DECIMAL(18,6) NOT NULL, /* piezas/porciones */
  PESO_GR            DECIMAL(18,6),         /* gramos si aplica báscula */
  PRECIO_UNIT        DECIMAL(18,6) NOT NULL,
  IMPORTE            DECIMAL(18,6) NOT NULL,
  CONSTRAINT FK_PDET_PED FOREIGN KEY (ID_PEDIDO) REFERENCES PEDIDOS(ID_PEDIDO)
);");

            // RECETAS (BOM de platillos) y detalle
            Exec(@"
CREATE TABLE RECETAS (
  ID_RECETA           INTEGER NOT NULL PRIMARY KEY,
  CLAVE_PLATILLO_SAE  VARCHAR(30) NOT NULL,
  NOMBRE_PLATILLO     VARCHAR(80) NOT NULL,
  MERMA_PCT           DECIMAL(9,4) DEFAULT 0
);");

            Exec(@"
CREATE TABLE RECETA_DET (
  ID_RECETA_DET     INTEGER NOT NULL PRIMARY KEY,
  ID_RECETA         INTEGER NOT NULL,
  CLAVE_INSUMO_SAE  VARCHAR(30) NOT NULL,
  CANT_GRAMOS       DECIMAL(18,6) NOT NULL,
  CONSTRAINT FK_RDET_REC FOREIGN KEY (ID_RECETA) REFERENCES RECETAS(ID_RECETA)
);");

            // INSUMO_EXT (datos extra para líquidos/pesables)
            Exec(@"
CREATE TABLE INSUMO_EXT (
  CLAVE_INSUMO_SAE   VARCHAR(30) NOT NULL PRIMARY KEY,
  TIPO               VARCHAR(10) NOT NULL, /* SOLIDO | LIQUIDO */
  DENSIDAD_G_POR_ML  DECIMAL(10,6),        /* requerido si LIQUIDO */
  UNIDAD_BASE_SAE    VARCHAR(10)
);");

            // BASCULA_LECTURAS (log auditoría)
            Exec(@"
CREATE TABLE BASCULA_LECTURAS (
  ID_LECTURA   INTEGER NOT NULL PRIMARY KEY,
  FECHA_HORA   TIMESTAMP NOT NULL,
  PUERTO       VARCHAR(20) NOT NULL,
  PESO_GR      DECIMAL(18,6) NOT NULL,
  ESTABLE      SMALLINT NOT NULL, /* 1=Sí, 0=No */
  ID_PEDIDO_DET INTEGER,
  CONSTRAINT FK_BAS_PDET FOREIGN KEY (ID_PEDIDO_DET) REFERENCES PEDIDO_DET(ID_PEDIDO_DET)
);");

            // CONFIG (clave-valor)
            Exec(@"
CREATE TABLE CONFIG (
  CLAVE VARCHAR(50) NOT NULL PRIMARY KEY,
  VALOR VARCHAR(255) NOT NULL
);");

            // ====== TRIGGERS AUTOINCREMENT ======
            Exec(@"
CREATE TRIGGER BI_USUARIOS FOR USUARIOS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_USUARIO IS NULL) THEN NEW.ID_USUARIO = GEN_ID(GEN_USUARIOS_ID, 1);
  IF (NEW.FECHA_ALTA IS NULL)  THEN NEW.FECHA_ALTA  = CURRENT_TIMESTAMP;
  IF (NEW.ACTIVO IS NULL)      THEN NEW.ACTIVO      = 1;
END;");

            Exec(@"
CREATE TRIGGER BI_MESAS FOR MESAS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_MESA IS NULL) THEN NEW.ID_MESA = GEN_ID(GEN_MESAS_ID, 1);
  IF (NEW.ESTADO IS NULL)  THEN NEW.ESTADO  = 'LIBRE';
END;");

            Exec(@"
CREATE TRIGGER BI_MESEROS FOR MESEROS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_MESERO IS NULL) THEN NEW.ID_MESERO = GEN_ID(GEN_MESEROS_ID, 1);
  IF (NEW.ACTIVO IS NULL)    THEN NEW.ACTIVO    = 1;
END;");

            Exec(@"
CREATE TRIGGER BI_TURNOS FOR TURNOS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_TURNO IS NULL) THEN NEW.ID_TURNO = GEN_ID(GEN_TURNOS_ID, 1);
  IF (NEW.FECHA IS NULL)    THEN NEW.FECHA    = CURRENT_DATE;
END;");

            Exec(@"
CREATE TRIGGER BI_MESA_TURNO FOR MESA_TURNO
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_MESA_TURNO IS NULL) THEN NEW.ID_MESA_TURNO = GEN_ID(GEN_MESA_TURNO_ID, 1);
END;");

            Exec(@"
CREATE TRIGGER BI_PEDIDOS FOR PEDIDOS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_PEDIDO IS NULL)      THEN NEW.ID_PEDIDO   = GEN_ID(GEN_PEDIDOS_ID, 1);
  IF (NEW.FECHA_HORA IS NULL)     THEN NEW.FECHA_HORA  = CURRENT_TIMESTAMP;
  IF (NEW.ESTADO IS NULL)         THEN NEW.ESTADO      = 'ABIERTO';
  IF (NEW.FACTURAR_AHORA IS NULL) THEN NEW.FACTURAR_AHORA = 0;
END;");

            Exec(@"
CREATE TRIGGER BI_PEDIDO_DET FOR PEDIDO_DET
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_PEDIDO_DET IS NULL) THEN NEW.ID_PEDIDO_DET = GEN_ID(GEN_PEDIDO_DET_ID, 1);
  IF (NEW.CANTIDAD IS NULL)      THEN NEW.CANTIDAD = 1;
  IF (NEW.IMPORTE IS NULL)       THEN NEW.IMPORTE  = 0;
END;");

            Exec(@"
CREATE TRIGGER BI_RECETAS FOR RECETAS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_RECETA IS NULL) THEN NEW.ID_RECETA = GEN_ID(GEN_RECETAS_ID, 1);
  IF (NEW.MERMA_PCT IS NULL) THEN NEW.MERMA_PCT = 0;
END;");

            Exec(@"
CREATE TRIGGER BI_RECETA_DET FOR RECETA_DET
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_RECETA_DET IS NULL) THEN NEW.ID_RECETA_DET = GEN_ID(GEN_RECETA_DET_ID, 1);
END;");

            Exec(@"
CREATE TRIGGER BI_BASCULA_LECTURAS FOR BASCULA_LECTURAS
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_LECTURA IS NULL) THEN NEW.ID_LECTURA = GEN_ID(GEN_BASCULA_LECT_ID, 1);
  IF (NEW.FECHA_HORA IS NULL) THEN NEW.FECHA_HORA = CURRENT_TIMESTAMP;
END;");

            // ====== ÍNDICES ======
            Exec("CREATE INDEX IX_MT_TURNO  ON MESA_TURNO (ID_TURNO);");
            Exec("CREATE INDEX IX_MT_MESA   ON MESA_TURNO (ID_MESA);");
            Exec("CREATE INDEX IX_MT_MESERO ON MESA_TURNO (ID_MESERO);");

            Exec("CREATE INDEX IX_PED_MT    ON PEDIDOS (ID_MESA_TURNO);");
            Exec("CREATE INDEX IX_PDET_PED  ON PEDIDO_DET (ID_PEDIDO);");

            Exec("CREATE INDEX IX_RDET_REC  ON RECETA_DET (ID_RECETA);");

            Exec("CREATE INDEX IX_BAS_PDET  ON BASCULA_LECTURAS (ID_PEDIDO_DET);");

            // Restricciones simples (boolean-like)
            Exec("ALTER TABLE USUARIOS ADD CONSTRAINT CK_USU_ACTIVO CHECK (ACTIVO IN (0,1));");
            Exec("ALTER TABLE MESEROS  ADD CONSTRAINT CK_MES_ACTIVO CHECK (ACTIVO IN (0,1));");
            Exec("ALTER TABLE PEDIDOS  ADD CONSTRAINT CK_PED_FACT CHECK (FACTURAR_AHORA IN (0,1));");
            Exec("ALTER TABLE BASCULA_LECTURAS ADD CONSTRAINT CK_BAS_ESTABLE CHECK (ESTABLE IN (0,1));");

            tx.Commit();
        }

        private static void SeedConfig(FbConnection conn)
        {
            // Claves sugeridas
            var seed = new (string Clave, string Valor)[]
            {
                ("ALMACEN_DEFAULT", "1"),
                ("IMPUESTO_PORC", "16"),
                ("IMPRESORA_TICKET", ""),
                ("SAE_FDB", ""),        // actualizaremos con UpsertConfig() desde la app
                ("BASCULA_PUERTO", "COM1"),
                ("LISTA_PRECIOS", "1")
            };

            foreach (var kv in seed)
                UpsertConfig(conn, kv.Clave, kv.Valor);

            // Usuario admin por defecto (hash/salt vacíos por ahora, lo ajustarás cuando implementes login)
            using var cmd = new FbCommand(@"
INSERT INTO USUARIOS (USERNAME, PASSWORD_HASH, PASSWORD_SALT, NOMBRE_COMPLETO, EMAIL, ACTIVO, EMPRESA_DEF, ROL_PRESET, FECHA_ALTA)
VALUES ('admin', '', '', 'Administrador', '', 1, 1, 'ADMIN', CURRENT_TIMESTAMP);", conn);
            cmd.ExecuteNonQuery();
        }

        public static void EnsureMovInvAux(FbConnection conn)
        {
            // Checa si existe la tabla
            bool exists;
            using (var cmd = new FbCommand(@"
SELECT 1 FROM RDB$RELATIONS 
WHERE RDB$SYSTEM_FLAG = 0 
AND RDB$RELATION_NAME = 'MOV_INV_AUX'", conn))
            {
                exists = cmd.ExecuteScalar() != null;
            }
            if (exists) return;

            using var tx = conn.BeginTransaction();
            void Exec(string sql)
            {
                using var c = new FbCommand(sql, conn, tx);
                c.ExecuteNonQuery();
            }

            Exec("CREATE SEQUENCE GEN_MOV_INV_AUX_ID;");
            Exec(@"
CREATE TABLE MOV_INV_AUX (
  ID_MOV              INTEGER NOT NULL PRIMARY KEY,
  FECHA_HORA          TIMESTAMP NOT NULL,
  TIPO                VARCHAR(10) NOT NULL, /* ENTRADA | SALIDA */
  CLAVE_ARTICULO_SAE  VARCHAR(30) NOT NULL,
  PESO_GR             DECIMAL(18,6) NOT NULL,
  COSTO_KG            DECIMAL(18,6),
  IMPORTE             DECIMAL(18,6),
  ORIGEN              VARCHAR(20),         /* BASCULA | MANUAL */
  POST_SAE            SMALLINT NOT NULL,   /* 0 | 1 */
  POST_SAE_FECHA      TIMESTAMP
);");
            Exec(@"
CREATE TRIGGER BI_MOV_INV_AUX FOR MOV_INV_AUX
ACTIVE BEFORE INSERT POSITION 0
AS
BEGIN
  IF (NEW.ID_MOV IS NULL)     THEN NEW.ID_MOV = GEN_ID(GEN_MOV_INV_AUX_ID, 1);
  IF (NEW.FECHA_HORA IS NULL) THEN NEW.FECHA_HORA = CURRENT_TIMESTAMP;
  IF (NEW.TIPO IS NULL)       THEN NEW.TIPO = 'ENTRADA';
  IF (NEW.POST_SAE IS NULL)   THEN NEW.POST_SAE = 0;
END;");
            tx.Commit();
        }

    }

}
