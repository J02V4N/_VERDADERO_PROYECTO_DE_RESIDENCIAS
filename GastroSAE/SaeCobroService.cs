using FirebirdSql.Data.FirebirdClient;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace GastroSAE
{
    public sealed class CobroRequest
    {
        public decimal Efectivo { get; set; }
        public decimal Tarjeta { get; set; }
        public string? ReferenciaTarjeta { get; set; }
        public bool FacturarAhora { get; set; }
        public string? Rfc { get; set; }
        public string? RazonSocial { get; set; }
        public string? UsoCfdi { get; set; }
        public string? MetodoPago { get; set; }
        public string? FormaPago { get; set; }
        public string? ClienteClaveSae { get; set; }
        public string? Almacen { get; set; }
        public string? CodigoPostal { get; set; }
        public string? RegFiscal { get; set; }
    }

    public sealed class CobroSnapshot
    {
        public int IdPedido { get; set; }
        public int IdMesa { get; set; }
        public int IdMesaTurno { get; set; }
        public DateTime FechaHora { get; set; }
        public string EstadoPedido { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal Impuesto { get; set; }
        public decimal Total { get; set; }
        public List<CobroLineaSnapshot> Lineas { get; set; } = new();
    }

    public sealed class CobroLineaSnapshot
    {
        public int IdPedidoDet { get; set; }
        public string ClaveArticuloSae { get; set; } = string.Empty;
        public bool EsPlatillo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal? PesoGr { get; set; }
        public decimal PrecioUnit { get; set; }
        public decimal Importe { get; set; }
    }

    public sealed class SaeCobroValidationResult
    {
        public List<string> Errors { get; } = new();
        public List<string> Warnings { get; } = new();
        public bool IsValid => Errors.Count == 0;

        public string ToDisplayText()
        {
            var sb = new StringBuilder();

            if (Errors.Count > 0)
            {
                sb.AppendLine("Errores detectados:");
                foreach (var err in Errors)
                    sb.AppendLine("- " + err);
            }

            if (Warnings.Count > 0)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.AppendLine("Advertencias:");
                foreach (var warn in Warnings)
                    sb.AppendLine("- " + warn);
            }

            return sb.ToString().Trim();
        }
    }

    public sealed class SaeCobroPreparation
    {
        public CobroSnapshot Snapshot { get; init; } = new();
        public CobroRequest Request { get; init; } = new();
        public SaeCobroValidationResult Validation { get; init; } = new();
    }

    public sealed class SaeCobroPostResult
    {
        public string ClienteClaveSae { get; set; } = string.Empty;
        public string NotaVentaDoc { get; set; } = string.Empty;
        public string NotaVentaSerie { get; set; } = string.Empty;
        public int NotaVentaFolio { get; set; }
        public string? FacturaDoc { get; set; }
        public string? FacturaSerie { get; set; }
        public int? FacturaFolio { get; set; }
        public List<string> Warnings { get; } = new();
    }

    internal sealed class SaeDocumentNumber
    {
        public string TipDoc { get; set; } = string.Empty;
        public string Serie { get; set; } = string.Empty;
        public int Folio { get; set; }
        public int Longitud { get; set; } = 10;
        public string CveDoc { get; set; } = string.Empty;
    }

    internal sealed class SaeClientInfo
    {
        public string Clave { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Rfc { get; set; } = string.Empty;
        public string? UsoCfdi { get; set; }
        public string? MetodoPago { get; set; }
        public string? FormaPagoSat { get; set; }
        public string? RegFiscal { get; set; }
        public string? CodigoPostal { get; set; }
        public string? Pais { get; set; }
        public string? CvePaisSat { get; set; }
        public int? ListaPrecios { get; set; }
    }

    internal sealed class SaeArticleInfo
    {
        public string Clave { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string UnidadVenta { get; set; } = string.Empty;
        public decimal Costo { get; set; }
        public string TipoProd { get; set; } = "P";
        public string TipoElem { get; set; } = "P";
        public string? CveProdServ { get; set; }
        public string? CveUnidad { get; set; }
        public int? CveEsqImpu { get; set; }
    }

    /// <summary>
    /// Flujo de cobro orientado a SAE.
    /// Registra el documento comercial en la BD de SAE (nota de venta y, si aplica,
    /// factura enlazada) y aplica el descuento operativo de inventario en MINVE/INVE.
    /// </summary>
    public static class SaeCobroService
    {
        private const string CfgClienteMostradorClave = "SAE_CLIENTE_MOSTRADOR_CLAVE";
        private const string CfgClienteMostradorNombre = "SAE_CLIENTE_MOSTRADOR_NOMBRE";
        private const string CfgClienteMostradorRfc = "SAE_CLIENTE_MOSTRADOR_RFC";
        private const string CfgClienteMostradorUsoCfdi = "SAE_CLIENTE_MOSTRADOR_USO_CFDI";
        private const string CfgClienteMostradorMetodoPago = "SAE_CLIENTE_MOSTRADOR_METODO_PAGO";
        private const string CfgClienteMostradorFormaPago = "SAE_CLIENTE_MOSTRADOR_FORMA_PAGO";
        private const string CfgClienteMostradorRegFiscal = "SAE_CLIENTE_MOSTRADOR_REG_FISC";
        private const string CfgSerieNota = "SAE_SERIE_NOTA";
        private const string CfgSerieFactura = "SAE_SERIE_FACTURA";
        private const string CfgConceptoSalida = "SAE_CONCEPTO_SALIDA_VENTA";

        public static SaeCobroPreparation PrepararDesdePedidoAuxiliar(int idPedido, CobroRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            EnsureCobroDefaultsInAuxConfig();
            var snapshot = AuxRepo.GetCobroSnapshot(idPedido);

            using var sae = SaeDb.GetOpenConnection();
            var validation = ValidarEntornoSae(sae, snapshot, request);

            return new SaeCobroPreparation
            {
                Snapshot = snapshot,
                Request = request,
                Validation = validation
            };
        }

        public static SaeCobroPostResult PostearCobroDesdePedidoAuxiliar(int idPedido, CobroRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            EnsureCobroDefaultsInAuxConfig();
            var snapshot = AuxRepo.GetCobroSnapshot(idPedido);

            using var con = SaeDb.GetOpenConnection();
            var validation = ValidarEntornoSae(con, snapshot, request);
            if (!validation.IsValid)
                throw new InvalidOperationException(validation.ToDisplayText());

            using var tx = con.BeginTransaction();
            try
            {
                var result = new SaeCobroPostResult();

                string inve = SaeDb.GetTableName(con, "INVE", tx);
                string clie = SaeDb.GetTableName(con, "CLIE", tx);
                string factv = SaeDb.GetTableName(con, "FACTV", tx);
                string parFactv = SaeDb.GetTableName(con, "PAR_FACTV", tx);
                string foliosf = SaeDb.GetTableName(con, "FOLIOSF", tx);
                string paramFoliosf = SaeDb.GetTableName(con, "PARAM_FOLIOSF", tx);
                string conM = SaeDb.GetTableName(con, "CONM", tx);
                string minve = SaeDb.GetTableName(con, "MINVE", tx);
                string mult = string.Empty;
                string? kits = snapshot.Lineas.Any(x => x.EsPlatillo) ? SaeDb.GetTableName(con, "KITS", tx) : null;

                string? factf = request.FacturarAhora ? SaeDb.GetTableName(con, "FACTF", tx) : null;
                string? parFactf = request.FacturarAhora ? SaeDb.GetTableName(con, "PAR_FACTF", tx) : null;
                string? doctosigf = request.FacturarAhora ? SaeDb.GetTableName(con, "DOCTOSIGF", tx) : null;

                int almacen = ParseWarehouseNumberOrThrow(request.Almacen);
                var client = request.FacturarAhora
                    ? EnsureFacturaClient(con, tx, clie, request)
                    : EnsureMostradorClient(con, tx, clie);

                result.ClienteClaveSae = client.Clave;

                int nextBitacora = ReadNextBitacoraCounter(con, tx, factv, factf);
                var notaDoc = ReserveNextDocumentNumber(con, tx, foliosf, paramFoliosf, "V", GetConfigOrDefault(CfgSerieNota, "STAND."));
                var documentLines = BuildDocumentLines(con, tx, inve, kits, snapshot);

                InsertSaleHeader(con, tx, factv, notaDoc, snapshot, request, client, almacen, isFactura: false,
                    docAnteriorTipo: null, docAnterior: null, enlazado: request.FacturarAhora ? "S" : "O", tipDocEnlazado: "O", bitacora: nextBitacora++);

                ApplyInventoryDiscount(con, tx, client, notaDoc, almacen, inve, mult, minve, conM, documentLines);
                InsertSaleLines(con, tx, parFactv, notaDoc, documentLines, almacen, affectInventory: true, isFactura: false);

                result.NotaVentaDoc = notaDoc.CveDoc;
                result.NotaVentaSerie = notaDoc.Serie;
                result.NotaVentaFolio = notaDoc.Folio;

                if (request.FacturarAhora)
                {
                    if (factf == null || parFactf == null)
                        throw new InvalidOperationException("No encontré las tablas de factura en SAE.");

                    var facturaDoc = ReserveNextDocumentNumber(con, tx, foliosf, paramFoliosf, "F", GetConfigOrDefault(CfgSerieFactura, "STAND."));
                    InsertSaleHeader(con, tx, factf, facturaDoc, snapshot, request, client, almacen, isFactura: true,
                        docAnteriorTipo: "V", docAnterior: notaDoc.CveDoc, enlazado: "O", tipDocEnlazado: "V", bitacora: nextBitacora++);
                    InsertSaleLines(con, tx, parFactf, facturaDoc, documentLines, almacen, affectInventory: false, isFactura: true);
                    UpdateDocumentLink(con, tx, factv, notaDoc.CveDoc, "F", facturaDoc.CveDoc);
                    if (!string.IsNullOrWhiteSpace(doctosigf))
                        InsertDocumentCrossLinks(con, tx, doctosigf!, notaDoc.CveDoc, facturaDoc.CveDoc, documentLines);

                    result.FacturaDoc = facturaDoc.CveDoc;
                    result.FacturaSerie = facturaDoc.Serie;
                    result.FacturaFolio = facturaDoc.Folio;
                }

                result.Warnings.AddRange(validation.Warnings);
                result.Warnings.Add("Descuento de inventario aplicado en SAE usando MINVE/INVE.");

                tx.Commit();
                return result;
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static void MarcarNotaVentaImpresa(string notaDoc)
        {
            if (string.IsNullOrWhiteSpace(notaDoc)) return;

            using var con = SaeDb.GetOpenConnection();
            using var tx = con.BeginTransaction();
            try
            {
                var factv = SaeDb.GetTableName(con, "FACTV", tx);
                var parFactv = SaeDb.GetTableName(con, "PAR_FACTV", tx);

                if (ColumnExists(con, factv, "FORMAENVIO", tx))
                {
                    using var up1 = new FbCommand($"UPDATE {factv} SET FORMAENVIO='I' WHERE CVE_DOC=@D", con, tx);
                    up1.Parameters.Add("@D", FbDbType.VarChar, 20).Value = notaDoc;
                    up1.ExecuteNonQuery();
                }

                if (ColumnExists(con, parFactv, "IMPRIMIR", tx))
                {
                    using var up2 = new FbCommand($"UPDATE {parFactv} SET IMPRIMIR='S' WHERE CVE_DOC=@D", con, tx);
                    up2.Parameters.Add("@D", FbDbType.VarChar, 20).Value = notaDoc;
                    up2.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

        public static SaeCobroValidationResult ValidarEntornoSae(FbConnection con, CobroSnapshot snapshot, CobroRequest request)
        {
            if (con == null) throw new ArgumentNullException(nameof(con));
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (request == null) throw new ArgumentNullException(nameof(request));

            EnsureCobroDefaultsInAuxConfig();

            var result = new SaeCobroValidationResult();

            string inve = GetRequiredTable(con, "INVE", result);
            string clie = GetRequiredTable(con, "CLIE", result);
            string factv = GetRequiredTable(con, "FACTV", result);
            string parFactv = GetRequiredTable(con, "PAR_FACTV", result);
            string foliosf = GetRequiredTable(con, "FOLIOSF", result);
            string paramFoliosf = GetRequiredTable(con, "PARAM_FOLIOSF", result);
            string conM = GetRequiredTable(con, "CONM", result);
            string minve = GetRequiredTable(con, "MINVE", result);
            string? kits = snapshot.Lineas.Any(x => x.EsPlatillo) ? GetRequiredTable(con, "KITS", result) : GetOptionalTable(con, "KITS", result);

            string? factf = null;
            string? parFactf = null;
            if (request.FacturarAhora)
            {
                factf = GetRequiredTable(con, "FACTF", result);
                parFactf = GetRequiredTable(con, "PAR_FACTF", result);
            }

            GetOptionalTable(con, "TBLCONTROL", result);
            GetOptionalTable(con, "DOCTOSIGF", result);
            GetOptionalTable(con, "ALMACENES", result);
            GetOptionalTable(con, "LTPD", result);
            GetOptionalTable(con, "ENLACE_LTPD", result);
            GetOptionalTable(con, "CAPAS_X_MOV", result);

            if (snapshot.Lineas.Count == 0)
                result.Errors.Add("El pedido no tiene partidas. No se puede cobrar ni postear a SAE.");

            if (!string.IsNullOrWhiteSpace(inve))
                ValidateArticlesExist(con, inve, snapshot, result);

            var almacen = (request.Almacen ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(almacen))
                almacen = (GetConfigOrDefault("ALMACEN_DEFAULT", "1") ?? "1").Trim();

            if (string.IsNullOrWhiteSpace(almacen))
                result.Errors.Add("No hay almacén configurado para el cobro. Revisa ALMACEN_DEFAULT.");
            else
                ValidateWarehouse(con, almacen, result);

            ValidateSalesTableShape(con, factv, parFactv, foliosf, paramFoliosf, result, "V");
            if (request.FacturarAhora && factf != null && parFactf != null)
                ValidateSalesTableShape(con, factf, parFactf, foliosf, paramFoliosf, result, "F");

            ValidateInventoryPostingSetup(con, inve, minve, conM, kits, snapshot, result);
            ValidateCustomer(con, clie, request, result);
            ValidatePaymentData(request, snapshot, result);
            ValidateFolioSeries(con, foliosf, paramFoliosf, request, result);

            return result;
        }

        public static string BuildNextImplementationGuide(SaeCobroPreparation preparation)
        {
            if (preparation == null) throw new ArgumentNullException(nameof(preparation));

            var sb = new StringBuilder();
            sb.AppendLine("Base lista del cobro SAE.");
            sb.AppendLine($"Pedido: {preparation.Snapshot.IdPedido}");
            sb.AppendLine($"Mesa: {preparation.Snapshot.IdMesa}");
            sb.AppendLine($"Total: {preparation.Snapshot.Total:N2}");
            sb.AppendLine($"Partidas: {preparation.Snapshot.Lineas.Count}");

            if (!preparation.Validation.IsValid)
            {
                sb.AppendLine();
                sb.AppendLine(preparation.Validation.ToDisplayText());
                return sb.ToString().Trim();
            }

            sb.AppendLine();
            sb.AppendLine("La base del cobro ya registra nota/factura y descuenta inventario en SAE.");
            sb.AppendLine("Pendiente principal: validar en tu SAE real el comportamiento de MINVE y los ajustes posteriores (cancelación/devolución). ");
            return sb.ToString().Trim();
        }

        private static void EnsureCobroDefaultsInAuxConfig()
        {
            string auxPath;
            using var aux = AuxDbInitializer.EnsureCreated(out auxPath, charset: "ISO8859_1");
            NormalizeMostradorDefaults(aux);
            EnsureConfigValue(aux, CfgClienteMostradorRfc, "XAXX010101000");
            EnsureConfigValue(aux, CfgClienteMostradorUsoCfdi, "S01");
            EnsureConfigValue(aux, CfgClienteMostradorMetodoPago, "PUE");
            EnsureConfigValue(aux, CfgClienteMostradorFormaPago, "01");
            EnsureConfigValue(aux, CfgClienteMostradorRegFiscal, string.Empty);
            EnsureConfigValue(aux, CfgSerieNota, "STAND.");
            EnsureConfigValue(aux, CfgSerieFactura, "STAND.");
            EnsureConfigValue(aux, CfgConceptoSalida, "51");
        }

        private static void NormalizeMostradorDefaults(FbConnection aux)
        {
            var claveActual = AuxDbInitializer.GetConfig(aux, CfgClienteMostradorClave)?.Trim();
            if (string.IsNullOrWhiteSpace(claveActual) || string.Equals(claveActual, "MOSTRADOR", StringComparison.OrdinalIgnoreCase))
                AuxDbInitializer.UpsertConfig(aux, CfgClienteMostradorClave, "MOSTR");

            var nombreActual = AuxDbInitializer.GetConfig(aux, CfgClienteMostradorNombre)?.Trim();
            if (string.IsNullOrWhiteSpace(nombreActual)
                || string.Equals(nombreActual, "MOSTRADOR", StringComparison.OrdinalIgnoreCase)
                || string.Equals(nombreActual, "PUBLICO EN GENERAL", StringComparison.OrdinalIgnoreCase))
                AuxDbInitializer.UpsertConfig(aux, CfgClienteMostradorNombre, "MOSTR");
        }

        private static void EnsureConfigValue(FbConnection aux, string key, string defaultValue)
        {
            var current = AuxDbInitializer.GetConfig(aux, key);
            if (current == null)
                AuxDbInitializer.UpsertConfig(aux, key, defaultValue);
        }

        private static string GetConfigOrDefault(string key, string defaultValue)
        {
            var value = AuxDbInitializer.GetConfig(key);
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        private static void ValidateArticlesExist(FbConnection con, string inveTable, CobroSnapshot snapshot, SaeCobroValidationResult result)
        {
            const string col = "CVE_ART";
            if (!ColumnExists(con, inveTable, col))
            {
                result.Errors.Add($"La tabla {inveTable} no tiene la columna {col}. No se puede validar el pedido contra SAE.");
                return;
            }

            using var cmd = new FbCommand($"SELECT COUNT(*) FROM {inveTable} WHERE CVE_ART = @C", con);
            var p = cmd.Parameters.Add("@C", FbDbType.VarChar);

            foreach (var linea in snapshot.Lineas)
            {
                p.Value = linea.ClaveArticuloSae.Trim();
                var count = Convert.ToInt32(cmd.ExecuteScalar());
                if (count <= 0)
                    result.Errors.Add($"La clave '{linea.ClaveArticuloSae}' del pedido no existe en {inveTable}.");
            }
        }

        private static void ValidateWarehouse(FbConnection con, string warehouseText, SaeCobroValidationResult result)
        {
            var parsed = ParseWarehouseNumber(warehouseText);
            if (!parsed.HasValue)
            {
                result.Errors.Add($"El almacén configurado '{warehouseText}' no tiene un número válido.");
                return;
            }

            var almTable = GetTableIfExists(con, "ALMACENES");
            if (string.IsNullOrWhiteSpace(almTable))
            {
                result.Warnings.Add("No encontré la tabla ALMACENES## para validar el almacén. Se asume que el valor configurado es correcto.");
                return;
            }

            if (!ColumnExists(con, almTable, "CVE_ALM"))
            {
                result.Warnings.Add($"La tabla {almTable} no tiene CVE_ALM. No pude validar el almacén configurado.");
                return;
            }

            using var cmd = new FbCommand($"SELECT COUNT(*) FROM {almTable} WHERE CVE_ALM = @A", con);
            cmd.Parameters.Add("@A", FbDbType.Integer).Value = parsed.Value;
            var count = Convert.ToInt32(cmd.ExecuteScalar());
            if (count <= 0)
                result.Errors.Add($"El almacén {parsed.Value} no existe en {almTable}.");
        }

        private static void ValidateInventoryPostingSetup(FbConnection con, string inveTable, string minveTable, string conMTable, string? kitsTable, CobroSnapshot snapshot, SaeCobroValidationResult result)
        {
            ValidateColumns(con, minveTable, result, isWarning: false,
                "CVE_ART", "ALMACEN", "NUM_MOV", "CVE_CPTO", "FECHA_DOCU", "REFER", "CANT", "COSTO", "SIGNO");
            ValidateColumns(con, conMTable, result, isWarning: false,
                "CVE_CPTO", "TIPO_MOV", "STATUS", "SIGNO");

            int conceptoSalida = ParseIntOrNull(GetConfigOrDefault(CfgConceptoSalida, "51")) ?? 51;
            if (!InventoryConceptExists(con, conMTable, conceptoSalida))
                result.Errors.Add($"No existe un concepto de salida válido en CONM## para el concepto {conceptoSalida}.");

            if (snapshot.Lineas.Any(x => x.EsPlatillo))
            {
                if (string.IsNullOrWhiteSpace(kitsTable))
                {
                    result.Errors.Add("El pedido contiene platillos, pero no encontré la tabla KITS## en SAE.");
                }
                else
                {
                    foreach (var platillo in snapshot.Lineas.Where(x => x.EsPlatillo))
                    {
                        if (!KitHasComponents(con, kitsTable, platillo.ClaveArticuloSae))
                            result.Errors.Add($"El platillo '{platillo.ClaveArticuloSae}' no tiene componentes configurados en KITS##.");
                    }
                }
            }
        }

        private static bool InventoryConceptExists(FbConnection con, string conMTable, int concepto, FbTransaction? tx = null)
        {
            using var cmd = tx == null
                ? new FbCommand($@"
SELECT COUNT(*)
FROM {conMTable}
WHERE CVE_CPTO = @C
  AND COALESCE(STATUS, 'A') = 'A'
  AND TIPO_MOV = 'S'
  AND COALESCE(SIGNO, -1) = -1", con)
                : new FbCommand($@"
SELECT COUNT(*)
FROM {conMTable}
WHERE CVE_CPTO = @C
  AND COALESCE(STATUS, 'A') = 'A'
  AND TIPO_MOV = 'S'
  AND COALESCE(SIGNO, -1) = -1", con, tx);
            cmd.Parameters.Add("@C", FbDbType.Integer).Value = concepto;
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static bool KitHasComponents(FbConnection con, string kitsTable, string cveArt, FbTransaction? tx = null)
        {
            using var cmd = tx == null
                ? new FbCommand($@"SELECT COUNT(*) FROM {kitsTable} WHERE CVE_ART = @C", con)
                : new FbCommand($@"SELECT COUNT(*) FROM {kitsTable} WHERE CVE_ART = @C", con, tx);
            cmd.Parameters.Add("@C", FbDbType.VarChar).Value = cveArt;
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void ValidateCustomer(FbConnection con, string clieTable, CobroRequest request, SaeCobroValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(clieTable))
                return;

            ValidateColumns(con, clieTable, result, isWarning: false, "CLAVE", "STATUS");
            ValidateColumns(con, clieTable, result, isWarning: true, "NOMBRE", "RFC", "USO_CFDI", "METODODEPAGO", "FORMADEPAGOSAT", "REG_FISC", "CODIGO", "PAIS");

            if (request.FacturarAhora)
            {
                if (string.IsNullOrWhiteSpace(request.Rfc) || string.IsNullOrWhiteSpace(request.RazonSocial))
                    result.Errors.Add("El cobro está marcado para facturar, pero faltan RFC o Razón Social.");
                else if (!Regex.IsMatch(request.Rfc.Trim().ToUpperInvariant(), @"^([A-ZÑ&]{3,4})\d{6}[A-Z0-9]{3}$"))
                    result.Errors.Add("El RFC capturado no tiene formato válido.");

                if (string.IsNullOrWhiteSpace(request.RegFiscal))
                    result.Errors.Add("El cobro está marcado para facturar, pero falta Régimen fiscal.");

                if (string.IsNullOrWhiteSpace(request.CodigoPostal))
                    result.Errors.Add("El cobro está marcado para facturar, pero falta Código postal.");
            }
        }

        private static void ValidatePaymentData(CobroRequest request, CobroSnapshot snapshot, SaeCobroValidationResult result)
        {
            var pagado = Math.Round(request.Efectivo + request.Tarjeta, 2);
            if (pagado <= 0)
                result.Errors.Add("El importe pagado es 0. No se puede cobrar.");

            if (pagado < Math.Round(snapshot.Total, 2))
                result.Errors.Add($"Pago insuficiente. Total {snapshot.Total:N2}, pagado {pagado:N2}.");

            if (!string.IsNullOrWhiteSpace(request.FormaPago) && request.FormaPago!.Length > 5)
                result.Errors.Add("FORMA_PAGO excede la longitud esperada.");

            if (!string.IsNullOrWhiteSpace(request.MetodoPago) && request.MetodoPago!.Length > 10)
                result.Errors.Add("METODO_PAGO excede la longitud esperada.");

            if (!string.IsNullOrWhiteSpace(request.UsoCfdi) && request.UsoCfdi!.Length > 5)
                result.Errors.Add("USO_CFDI excede la longitud esperada.");
        }

        private static void ValidateSalesTableShape(FbConnection con, string factTable, string parFactTable, string foliosf, string paramFoliosf, SaeCobroValidationResult result, string tipDoc)
        {
            ValidateColumns(con, factTable, result, isWarning: false,
                "TIP_DOC", "CVE_DOC", "CVE_CLPV", "FECHA_DOC", "CAN_TOT", "IMPORTE", "SERIE", "FOLIO");

            ValidateColumns(con, parFactTable, result, isWarning: false,
                "CVE_DOC", "NUM_PAR", "CVE_ART", "CANT", "PREC", "TOT_PARTIDA");

            ValidateColumns(con, foliosf, result, isWarning: true,
                "TIP_DOC", "SERIE", "ULT_DOC", "FOLIODESDE", "STATUS");

            ValidateColumns(con, paramFoliosf, result, isWarning: true,
                "TIPODOCTO", "SERIE", "LONGITUD", "FOLIOINICIAL");

            if (!SeriesExists(con, foliosf, tipDoc, GetPreferredSeriesForDoc(tipDoc)))
                result.Warnings.Add($"No encontré la serie preferida '{GetPreferredSeriesForDoc(tipDoc)}' para el documento {tipDoc}. Se usará la primera disponible.");
        }

        private static void ValidateFolioSeries(FbConnection con, string foliosf, string paramFoliosf, CobroRequest request, SaeCobroValidationResult result)
        {
            if (!SeriesExists(con, foliosf, "V", GetPreferredSeriesForDoc("V")))
                result.Errors.Add("No existe una serie activa para notas de venta en FOLIOSF##.");

            if (request.FacturarAhora && !SeriesExists(con, foliosf, "F", GetPreferredSeriesForDoc("F")))
                result.Errors.Add("No existe una serie activa para facturas en FOLIOSF##.");

            if (!TipodoctoConfigured(con, paramFoliosf, "V"))
                result.Warnings.Add("PARAM_FOLIOSF## no tiene configuración visible para notas de venta (V).");

            if (request.FacturarAhora && !TipodoctoConfigured(con, paramFoliosf, "F"))
                result.Warnings.Add("PARAM_FOLIOSF## no tiene configuración visible para facturas (F).");
        }

        private static string GetPreferredSeriesForDoc(string tipDoc)
        {
            return tipDoc == "F"
                ? GetConfigOrDefault(CfgSerieFactura, "STAND.")
                : GetConfigOrDefault(CfgSerieNota, "STAND.");
        }

        private static bool SeriesExists(FbConnection con, string foliosf, string tipDoc, string serie)
        {
            if (string.IsNullOrWhiteSpace(foliosf))
                return false;

            using var cmd = new FbCommand($@"
SELECT COUNT(*)
FROM {foliosf}
WHERE TIP_DOC = @T
  AND TRIM(SERIE) = @S
  AND COALESCE(STATUS, 'D') <> 'B'", con);
            cmd.Parameters.Add("@T", FbDbType.VarChar).Value = tipDoc;
            cmd.Parameters.Add("@S", FbDbType.VarChar).Value = serie.Trim();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static bool TipodoctoConfigured(FbConnection con, string paramFoliosf, string tipDoc)
        {
            if (string.IsNullOrWhiteSpace(paramFoliosf))
                return false;

            using var cmd = new FbCommand($@"
SELECT COUNT(*)
FROM {paramFoliosf}
WHERE TIPODOCTO = @T", con);
            cmd.Parameters.Add("@T", FbDbType.VarChar).Value = tipDoc;
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void InsertSaleHeader(
            FbConnection con,
            FbTransaction tx,
            string table,
            SaeDocumentNumber doc,
            CobroSnapshot snapshot,
            CobroRequest request,
            SaeClientInfo client,
            int almacen,
            bool isFactura,
            string? docAnteriorTipo,
            string? docAnterior,
            string enlazado,
            string? tipDocEnlazado,
            int bitacora)
        {
            var now = DateTime.Now;
            var fechaDocumento = snapshot.FechaHora.Date;
            var fechaEntrega = snapshot.FechaHora.Date;
            var fechaVencimiento = snapshot.FechaHora.Date;
            var rfcDocumento = isFactura ? string.Empty : string.Empty;
            var primerPago = isFactura ? 0m : snapshot.Total;
            var statusDoc = "E";
            string? condicionDoc = null;
            var enlazadoDoc = isFactura ? enlazado : "O";
            var tipDocEnlazadoDoc = isFactura ? tipDocEnlazado : "O";
            var serieDoc = string.Empty;
            var formaPagoSatDoc = isFactura ? string.Empty : string.Empty;
            var usoCfdiDoc = isFactura ? null : string.Empty;
            var regFiscalDoc = isFactura ? string.Empty : string.Empty;
            var numCtaPagoDoc = string.Empty;
            var tipDocAntDoc = isFactura ? (docAnteriorTipo ?? string.Empty) : string.Empty;
            var docAntDoc = isFactura ? (docAnterior ?? string.Empty) : string.Empty;
            var autoAnioDoc = string.Empty;
            string? tipTrasladoDoc = null;
            var metodoPagoDoc = isFactura ? "PUE" : "99 OTROS";
            var formaEnvioDoc = "I";

            var map = new Dictionary<string, object?>
            {
                ["TIP_DOC"] = doc.TipDoc,
                ["CVE_DOC"] = doc.CveDoc,
                ["CVE_CLPV"] = client.Clave,
                ["STATUS"] = statusDoc,
                ["DAT_MOSTR"] = bitacora,
                ["CVE_VEND"] = string.Empty,
                ["CVE_PEDI"] = string.Empty,
                ["FECHA_DOC"] = fechaDocumento,
                ["FECHA_ENT"] = fechaEntrega,
                ["FECHA_VEN"] = fechaVencimiento,
                ["CAN_TOT"] = snapshot.Subtotal,
                ["IMP_TOT1"] = 0m,
                ["IMP_TOT2"] = 0m,
                ["IMP_TOT3"] = 0m,
                ["IMP_TOT4"] = snapshot.Impuesto,
                ["IMP_TOT5"] = 0m,
                ["IMP_TOT6"] = 0m,
                ["IMP_TOT7"] = 0m,
                ["IMP_TOT8"] = 0m,
                ["DES_TOT"] = 0m,
                ["DES_FIN"] = 0m,
                ["COM_TOT"] = 0m,
                ["CONDICION"] = condicionDoc,
                ["CVE_OBS"] = 0,
                ["NUM_ALMA"] = almacen,
                ["ACT_CXC"] = "S",
                ["ACT_COI"] = "N",
                ["ENLAZADO"] = enlazadoDoc,
                ["TIP_DOC_E"] = tipDocEnlazadoDoc,
                ["NUM_MONED"] = 1,
                ["TIPCAMB"] = 1m,
                ["NUM_PAGOS"] = 1,
                ["FECHAELAB"] = now,
                ["PRIMERPAGO"] = primerPago,
                ["RFC"] = rfcDocumento,
                ["CTLPOL"] = 0,
                ["ESCFD"] = "N",
                ["AUTORIZA"] = 1,
                ["SERIE"] = serieDoc,
                ["FOLIO"] = doc.Folio,
                ["AUTOANIO"] = autoAnioDoc,
                ["DAT_ENVIO"] = 0,
                ["CONTADO"] = isFactura ? "N" : "S",
                ["CVE_BITA"] = bitacora,
                ["BLOQ"] = "N",
                ["FORMAENVIO"] = formaEnvioDoc,
                ["DES_FIN_PORC"] = 0m,
                ["DES_TOT_PORC"] = 0m,
                ["IMPORTE"] = snapshot.Total,
                ["COM_TOT_PORC"] = 0m,
                ["METODODEPAGO"] = metodoPagoDoc,
                ["NUMCTAPAGO"] = numCtaPagoDoc,
                ["TIP_DOC_ANT"] = tipDocAntDoc,
                ["DOC_ANT"] = docAntDoc,
                ["UUID"] = Guid.NewGuid().ToString().ToUpperInvariant(),
                ["VERSION_SINC"] = now,
                ["FORMADEPAGOSAT"] = formaPagoSatDoc,
                ["USO_CFDI"] = usoCfdiDoc,
                ["TIP_TRASLADO"] = tipTrasladoDoc,
                ["TIP_FAC"] = isFactura ? "F" : "V",
                ["REG_FISC"] = regFiscalDoc,
                ["CTLCOI"] = bitacora * 2 + 1
            };

            InsertDynamic(con, tx, table, map);
        }

        private sealed class DocumentLineWriteRow
        {
            public int NumPar { get; set; }
            public string CveArt { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public decimal Cantidad { get; set; }
            public decimal PrecioUnitBase { get; set; }
            public decimal TotalBase { get; set; }
            public decimal Iva { get; set; }
            public decimal Costo { get; set; }
            public string UnidadVenta { get; set; } = string.Empty;
            public string TipoProd { get; set; } = "P";
            public string TipoElem { get; set; } = "N";
            public bool IsInventoryLine { get; set; }
            public int NumMov { get; set; }
            public int Eltpd { get; set; }
            public decimal PrecioNeto { get; set; }
            public decimal PrecioConImp { get; set; }
            public string? CveProdServ { get; set; }
            public string? CveUnidad { get; set; }
            public int? CveEsq { get; set; }
            public bool IsPrimarySaleLine { get; set; }
        }

        private static List<DocumentLineWriteRow> BuildDocumentLines(
            FbConnection con,
            FbTransaction tx,
            string inveTable,
            string? kitsTable,
            CobroSnapshot snapshot)
        {
            var rows = new List<DocumentLineWriteRow>();
            var articleCache = new Dictionary<string, SaeArticleInfo>(StringComparer.OrdinalIgnoreCase);
            int partida = 1;

            SaeArticleInfo getArticle(string clave, bool esPlatillo)
            {
                if (!articleCache.TryGetValue(clave, out var article))
                {
                    article = ReadArticleInfo(con, tx, inveTable, clave, esPlatillo);
                    articleCache[clave] = article;
                }
                return article;
            }

            foreach (var linea in snapshot.Lineas)
            {
                var article = getArticle(linea.ClaveArticuloSae, linea.EsPlatillo);
                decimal cantidadDoc = GetDocumentQuantity(linea);
                decimal ivaLinea = Math.Round(linea.Importe * 0.16m, 2, MidpointRounding.AwayFromZero);
                decimal precioUnitConIva = Math.Round(linea.PrecioUnit * 1.16m, 2, MidpointRounding.AwayFromZero);

                rows.Add(new DocumentLineWriteRow
                {
                    NumPar = partida++,
                    CveArt = linea.ClaveArticuloSae,
                    Descripcion = article.Descripcion,
                    Cantidad = cantidadDoc,
                    PrecioUnitBase = linea.PrecioUnit,
                    TotalBase = linea.Importe,
                    Iva = ivaLinea,
                    Costo = article.Costo,
                    UnidadVenta = article.UnidadVenta,
                    TipoProd = linea.EsPlatillo ? "K" : article.TipoProd,
                    TipoElem = "N",
                    IsInventoryLine = !linea.EsPlatillo,
                    Eltpd = 0,
                    PrecioNeto = 0m,
                    PrecioConImp = precioUnitConIva,
                    CveProdServ = article.CveProdServ,
                    CveUnidad = article.CveUnidad,
                    CveEsq = article.CveEsqImpu,
                    IsPrimarySaleLine = true
                });

                if (!linea.EsPlatillo)
                    continue;

                if (string.IsNullOrWhiteSpace(kitsTable))
                    throw new InvalidOperationException("No encontré KITS## para expandir partidas de platillos.");

                decimal piezasVendidas = linea.Cantidad > 0m ? linea.Cantidad : 1m;
                var components = ReadKitComponents(con, tx, kitsTable, linea.ClaveArticuloSae)
                    .Select(c =>
                    {
                        c.CantidadBase = Math.Round(c.CantidadBase * piezasVendidas, 6, MidpointRounding.AwayFromZero);
                        return c;
                    })
                    .Where(c => c.CantidadBase > 0m)
                    .ToList();

                if (components.Count == 0)
                    continue;

                for (int idx = 0; idx < components.Count; idx++)
                {
                    var comp = components[idx];
                    decimal precioComp = Math.Round(comp.PrecioPublicoBase, 6, MidpointRounding.AwayFromZero);
                    decimal totalComp = precioComp > 0m
                        ? Math.Round(comp.CantidadBase * precioComp, 2, MidpointRounding.AwayFromZero)
                        : 0m;
                    decimal ivaComp = totalComp > 0m
                        ? Math.Round(totalComp * 0.16m, 2, MidpointRounding.AwayFromZero)
                        : 0m;

                    rows.Add(new DocumentLineWriteRow
                    {
                        NumPar = partida++,
                        CveArt = comp.ClaveArticulo,
                        Descripcion = comp.Descripcion,
                        Cantidad = comp.CantidadBase,
                        PrecioUnitBase = precioComp,
                        TotalBase = totalComp,
                        Iva = ivaComp,
                        Costo = comp.Costo,
                        UnidadVenta = comp.UnidadVenta,
                        TipoProd = string.IsNullOrWhiteSpace(comp.TipoProd) ? "P" : comp.TipoProd,
                        TipoElem = "K",
                        IsInventoryLine = true,
                        Eltpd = idx + 1,
                        PrecioNeto = 0m,
                        PrecioConImp = 0m,
                        CveProdServ = comp.CveProdServ,
                        CveUnidad = comp.CveUnidad,
                        CveEsq = comp.CveEsqImpu,
                        IsPrimarySaleLine = false
                    });
                }
            }

            return rows;
        }

        private static void InsertSaleLines(
            FbConnection con,
            FbTransaction tx,
            string table,
            SaeDocumentNumber doc,
            IEnumerable<DocumentLineWriteRow> lines,
            int almacen,
            bool affectInventory,
            bool isFactura)
        {
            foreach (var row in lines.OrderBy(x => x.NumPar))
            {
                var map = new Dictionary<string, object?>
                {
                    ["CVE_DOC"] = doc.CveDoc,
                    ["NUM_PAR"] = row.NumPar,
                    ["CVE_ART"] = row.CveArt,
                    ["CANT"] = row.Cantidad,
                    ["PXS"] = row.Cantidad,
                    ["PREC"] = row.PrecioUnitBase,
                    ["COST"] = row.Costo,
                    ["IMPU1"] = 0m,
                    ["IMPU2"] = 0m,
                    ["IMPU3"] = 0m,
                    ["IMPU4"] = row.Iva > 0m ? 16m : 0m,
                    ["IMPU5"] = 0m,
                    ["IMPU6"] = 0m,
                    ["IMPU7"] = 0m,
                    ["IMPU8"] = 0m,
                    ["IMP1APLA"] = 6,
                    ["IMP2APLA"] = 6,
                    ["IMP3APLA"] = 6,
                    ["IMP4APLA"] = 0,
                    ["IMP5APLA"] = 6,
                    ["IMP6APLA"] = 6,
                    ["IMP7APLA"] = 6,
                    ["IMP8APLA"] = 6,
                    ["TOTIMP1"] = 0m,
                    ["TOTIMP2"] = 0m,
                    ["TOTIMP3"] = 0m,
                    ["TOTIMP4"] = row.Iva,
                    ["TOTIMP5"] = 0m,
                    ["TOTIMP6"] = 0m,
                    ["TOTIMP7"] = 0m,
                    ["TOTIMP8"] = 0m,
                    ["DESC1"] = 0m,
                    ["DESC2"] = 0m,
                    ["DESC3"] = 0m,
                    ["COMI"] = 0m,
                    ["APAR"] = 0m,
                    ["ACT_INV"] = affectInventory ? "S" : "N",
                    ["NUM_ALM"] = almacen,
                    ["POLIT_APLI"] = string.Empty,
                    ["TIP_CAM"] = 1m,
                    ["UNI_VENTA"] = row.UnidadVenta,
                    ["TIPO_PROD"] = row.TipoProd,
                    ["CVE_OBS"] = 0,
                    ["REG_SERIE"] = 0,
                    ["E_LTPD"] = 0,
                    ["TIPO_ELEM"] = row.TipoElem,
                    ["NUM_MOV"] = (affectInventory && row.IsInventoryLine) ? row.NumMov : 0,
                    ["TOT_PARTIDA"] = row.TotalBase,
                    ["IMPRIMIR"] = "S",
                    ["MAN_IEPS"] = "N",
                    ["APL_MAN_IMP"] = 1,
                    ["CUOTA_IEPS"] = 0m,
                    ["APL_MAN_IEPS"] = "C",
                    ["MTO_PORC"] = row.IsInventoryLine ? null : 0m,
                    ["MTO_CUOTA"] = 0m,
                    ["CVE_ESQ"] = row.IsInventoryLine ? null : (row.CveEsq ?? 1),
                    ["DESCR_ART"] = null,
                    ["UUID"] = Guid.NewGuid().ToString().ToUpperInvariant(),
                    ["VERSION_SINC"] = DateTime.Now,
                    ["PREC_NETO"] = isFactura ? (row.IsPrimarySaleLine ? 0m : null) : null,
                    ["CVE_PRODSERV"] = row.IsInventoryLine ? string.Empty : null,
                    ["CVE_UNIDAD"] = row.IsInventoryLine ? string.Empty : null,
                    ["PRECCIMP"] = row.PrecioConImp
                };

                InsertDynamic(con, tx, table, map);
            }
        }

        private sealed class InventoryIssueRow
        {
            public string ClaveArticulo { get; set; } = string.Empty;
            public decimal Cantidad { get; set; }
            public decimal TotalBase { get; set; }
            public string UnidadVenta { get; set; } = string.Empty;
            public List<DocumentLineWriteRow> SourceLines { get; } = new();
        }

        private sealed class KitComponentInfo
        {
            public string ClaveArticulo { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public decimal CantidadBase { get; set; }
            public decimal Costo { get; set; }
            public decimal PrecioPublicoBase { get; set; }
            public string UnidadVenta { get; set; } = string.Empty;
            public string TipoProd { get; set; } = "P";
            public string? CveProdServ { get; set; }
            public string? CveUnidad { get; set; }
            public int? CveEsqImpu { get; set; }
        }

        private static void ApplyInventoryDiscount(
            FbConnection con,
            FbTransaction tx,
            SaeClientInfo client,
            SaeDocumentNumber notaDoc,
            int almacen,
            string inveTable,
            string multTable,
            string minveTable,
            string conMTable,
            List<DocumentLineWriteRow> documentLines)
        {
            int conceptoSalida = ParseIntOrNull(GetConfigOrDefault(CfgConceptoSalida, "51")) ?? 51;
            if (!InventoryConceptExists(con, conMTable, conceptoSalida, tx))
                throw new InvalidOperationException($"El concepto de salida {conceptoSalida} no existe o no está activo en CONM##.");

            var aggregated = new Dictionary<string, InventoryIssueRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var line in documentLines.Where(x => x.IsInventoryLine))
                AddOrAccumulateIssue(aggregated, line);

            if (aggregated.Count == 0)
                return;

            int nextNumMov = ReadNextInventoryMovementNumber(con, tx, minveTable);

            foreach (var issue in aggregated.Values.OrderBy(x => x.ClaveArticulo, StringComparer.OrdinalIgnoreCase))
            {
                if (issue.Cantidad <= 0m)
                    continue;

                var article = ReadArticleInfo(con, tx, inveTable, issue.ClaveArticulo, esPlatillo: false);
                var stock = ReadInventoryStock(con, tx, inveTable, multTable, issue.ClaveArticulo, almacen);

                decimal nuevaExistenciaGlobal = decimal.Round(stock.ExistenciaGlobal - issue.Cantidad, 6, MidpointRounding.AwayFromZero);
                decimal nuevaExistenciaAlmacen = nuevaExistenciaGlobal;
                if (nuevaExistenciaGlobal < 0m)
                    throw new InvalidOperationException($"Inventario insuficiente para '{issue.ClaveArticulo}'. Existencia actual {stock.ExistenciaGlobal:N3}, salida requerida {issue.Cantidad:N3}.");

                int numMov = nextNumMov++;
                decimal precioMovimiento = issue.Cantidad > 0m
                    ? decimal.Round(issue.TotalBase / issue.Cantidad, 6, MidpointRounding.AwayFromZero)
                    : 0m;

                InsertInventoryMovement(con, tx, minveTable, numMov, conceptoSalida, client, notaDoc, almacen, issue, article, stock, nuevaExistenciaGlobal, nuevaExistenciaAlmacen, precioMovimiento);
                UpdateInventoryStock(con, tx, inveTable, multTable, issue.ClaveArticulo, almacen, nuevaExistenciaGlobal, nuevaExistenciaAlmacen);

                foreach (var source in issue.SourceLines)
                    source.NumMov = numMov;
            }
        }

        private static void AddOrAccumulateIssue(IDictionary<string, InventoryIssueRow> aggregated, DocumentLineWriteRow line)
        {
            var clave = (line.CveArt ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(clave) || line.Cantidad <= 0m)
                return;

            if (!aggregated.TryGetValue(clave, out var row))
            {
                row = new InventoryIssueRow
                {
                    ClaveArticulo = clave,
                    Cantidad = 0m,
                    TotalBase = 0m,
                    UnidadVenta = line.UnidadVenta
                };
                aggregated[clave] = row;
            }

            row.Cantidad = decimal.Round(row.Cantidad + line.Cantidad, 6, MidpointRounding.AwayFromZero);
            row.TotalBase = decimal.Round(row.TotalBase + line.TotalBase, 2, MidpointRounding.AwayFromZero);
            if (string.IsNullOrWhiteSpace(row.UnidadVenta))
                row.UnidadVenta = line.UnidadVenta;
            row.SourceLines.Add(line);
        }

        private static List<KitComponentInfo> ReadKitComponents(FbConnection con, FbTransaction tx, string kitsTable, string cveArt)
        {
            var list = new List<KitComponentInfo>();
            string inveTable = SaeDb.GetTableName(con, "INVE", tx);
            string tPXP = SaeDb.GetTableName(con, "PRECIO_X_PROD", tx);

            using var cmd = new FbCommand($@"
SELECT
    TRIM(K.CVE_PROD),
    COALESCE(K.CANTIDAD, 0),
    TRIM(COALESCE(I.DESCR, '')),
    TRIM(COALESCE(I.UNI_MED, '')),
    TRIM(COALESCE(I.UNI_ALT, '')),
    COALESCE(I.FAC_CONV, 1),
    COALESCE(I.COSTO_PROM, 0),
    TRIM(COALESCE(I.CVE_PRODSERV, '')),
    TRIM(COALESCE(I.CVE_UNIDAD, '')),
    COALESCE(I.CVE_ESQIMPU, 1),
    TRIM(COALESCE(I.TIPO_ELE, 'P')),
    COALESCE(P.PRECIO, 0)
FROM {kitsTable} K
LEFT JOIN {inveTable} I ON I.CVE_ART = K.CVE_PROD
LEFT JOIN {tPXP} P ON P.CVE_ART = K.CVE_PROD AND P.CVE_PRECIO = 1
WHERE K.CVE_ART = @C
ORDER BY K.CVE_PROD", con, tx);
            cmd.Parameters.Add("@C", FbDbType.VarChar).Value = cveArt;
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var clave = rd.IsDBNull(0) ? string.Empty : rd.GetString(0).Trim();
                var cantidadCaptura = rd.IsDBNull(1) ? 0m : Convert.ToDecimal(rd.GetValue(1));
                if (string.IsNullOrWhiteSpace(clave) || cantidadCaptura <= 0m)
                    continue;

                var descr = rd.IsDBNull(2) ? clave : rd.GetString(2).Trim();
                var uniMed = rd.IsDBNull(3) ? string.Empty : rd.GetString(3).Trim();
                var uniAlt = rd.IsDBNull(4) ? string.Empty : rd.GetString(4).Trim();
                var facConv = rd.IsDBNull(5) ? 1m : Convert.ToDecimal(rd.GetValue(5));
                var costo = rd.IsDBNull(6) ? 0m : Convert.ToDecimal(rd.GetValue(6));
                var cveProdServ = rd.IsDBNull(7) ? null : NullIfWhiteSpace(rd.GetString(7).Trim());
                var cveUnidad = rd.IsDBNull(8) ? null : NullIfWhiteSpace(rd.GetString(8).Trim());
                var cveEsq = rd.IsDBNull(9) ? (int?)1 : Convert.ToInt32(rd.GetValue(9));
                var tipoProd = rd.IsDBNull(10) ? "P" : rd.GetString(10).Trim();
                var precioPublicoBase = rd.IsDBNull(11) ? 0m : Convert.ToDecimal(rd.GetValue(11));

                var profile = SaeCatalogAdmin.ResolveUnitProfile(uniMed, uniAlt, facConv);
                var cantidadBase = SaeCatalogAdmin.NormalizeKitQtyForRuntime(profile.UniAlt, profile.UniMed, profile.FacConv, cantidadCaptura);

                if (cantidadBase <= 0m)
                    continue;

                list.Add(new KitComponentInfo
                {
                    ClaveArticulo = clave,
                    Descripcion = descr,
                    CantidadBase = cantidadBase,
                    Costo = costo,
                    PrecioPublicoBase = precioPublicoBase,
                    UnidadVenta = profile.UniAlt,
                    TipoProd = string.IsNullOrWhiteSpace(tipoProd) ? "P" : tipoProd,
                    CveProdServ = cveProdServ,
                    CveUnidad = cveUnidad,
                    CveEsqImpu = cveEsq
                });
            }
            return list;
        }

        private sealed class InventoryStockInfo
        {
            public decimal ExistenciaGlobal { get; set; }
            public decimal ExistenciaAlmacen { get; set; }
            public decimal CostoPromedio { get; set; }
        }

        private static InventoryStockInfo ReadInventoryStock(FbConnection con, FbTransaction tx, string inveTable, string multTable, string cveArt, int almacen)
        {
            _ = multTable;
            _ = almacen;

            var info = new InventoryStockInfo();

            using (var cmd = new FbCommand($@"
SELECT COALESCE(EXIST, 0), COALESCE(COSTO_PROM, 0)
FROM {inveTable}
WHERE CVE_ART = @C", con, tx))
            {
                cmd.Parameters.Add("@C", FbDbType.VarChar).Value = cveArt;
                using var rd = cmd.ExecuteReader();
                if (!rd.Read())
                    throw new InvalidOperationException($"El artículo '{cveArt}' ya no existe en SAE.");
                info.ExistenciaGlobal = rd.IsDBNull(0) ? 0m : Convert.ToDecimal(rd.GetValue(0));
                info.CostoPromedio = rd.IsDBNull(1) ? 0m : Convert.ToDecimal(rd.GetValue(1));
            }

            info.ExistenciaAlmacen = info.ExistenciaGlobal;
            return info;
        }

        private static void EnsureArticuloEnAlmacen(FbConnection con, FbTransaction tx, string multTable, string inveTable, string cveArt, int almacen)
        {
            // Multi-almacén desactivado en SAE: no se inserta ni actualiza MULT##.
            _ = con;
            _ = tx;
            _ = multTable;
            _ = inveTable;
            _ = cveArt;
            _ = almacen;
        }

        private static int ReadNextInventoryMovementNumber(FbConnection con, FbTransaction tx, string minveTable)
        {
            using var cmd = new FbCommand($@"SELECT COALESCE(MAX(NUM_MOV), 0) FROM {minveTable}", con, tx);
            return Convert.ToInt32(cmd.ExecuteScalar()) + 1;
        }

        private static void InsertInventoryMovement(
            FbConnection con,
            FbTransaction tx,
            string minveTable,
            int numMov,
            int conceptoSalida,
            SaeClientInfo client,
            SaeDocumentNumber notaDoc,
            int almacen,
            InventoryIssueRow issue,
            SaeArticleInfo article,
            InventoryStockInfo stock,
            decimal nuevaExistenciaGlobal,
            decimal nuevaExistenciaAlmacen,
            decimal precioMovimiento)
        {
            var map = new Dictionary<string, object?>
            {
                ["CVE_ART"] = issue.ClaveArticulo,
                ["ALMACEN"] = almacen,
                ["NUM_MOV"] = numMov,
                ["CVE_CPTO"] = conceptoSalida,
                ["FECHA_DOCU"] = DateTime.Now.Date,
                ["TIPO_DOC"] = "N",
                ["REFER"] = notaDoc.CveDoc,
                ["CLAVE_CLPV"] = client.Clave,
                ["CANT"] = issue.Cantidad,
                ["CANT_COST"] = 0m,
                ["PRECIO"] = precioMovimiento,
                ["COSTO"] = article.Costo,
                ["REG_SERIE"] = 0,
                ["UNI_VENTA"] = string.IsNullOrWhiteSpace(issue.UnidadVenta) ? article.UnidadVenta : issue.UnidadVenta,
                ["E_LTPD"] = 0,
                ["EXIST_G"] = nuevaExistenciaGlobal,
                ["EXISTENCIA"] = nuevaExistenciaAlmacen,
                ["VEND"] = string.Empty,
                ["TIPO_PROD"] = null,
                ["FACTOR_CON"] = 1m,
                ["FECHAELAB"] = DateTime.Now,
                ["CVE_FOLIO"] = numMov.ToString(),
                ["SIGNO"] = -1,
                ["COSTEADO"] = "S",
                ["COSTO_PROM_INI"] = stock.CostoPromedio,
                ["COSTO_PROM_FIN"] = stock.CostoPromedio,
                ["COSTO_PROM_GRAL"] = stock.CostoPromedio,
                ["DESDE_INVE"] = "N",
                ["MOV_ENLAZADO"] = 0
            };
            InsertDynamic(con, tx, minveTable, map);
        }

        private static void UpdateInventoryStock(FbConnection con, FbTransaction tx, string inveTable, string multTable, string cveArt, int almacen, decimal nuevaExistenciaGlobal, decimal nuevaExistenciaAlmacen)
        {
            _ = multTable;
            _ = almacen;
            _ = nuevaExistenciaAlmacen;

            using (var cmd = new FbCommand($@"
UPDATE {inveTable}
SET EXIST = @EXIST,
    STATUS = 'A'
WHERE CVE_ART = @C", con, tx))
            {
                cmd.Parameters.Add("@EXIST", FbDbType.Double).Value = Convert.ToDouble(nuevaExistenciaGlobal);
                cmd.Parameters.Add("@C", FbDbType.VarChar).Value = cveArt;
                if (cmd.ExecuteNonQuery() <= 0)
                    throw new InvalidOperationException($"No se pudo actualizar EXIST en {inveTable} para '{cveArt}'.");
            }
        }

        private static void UpdateDocumentLink(FbConnection con, FbTransaction tx, string factvTable, string notaDoc, string tipDocSig, string docSig)
        {
            var sets = new List<string>();
            if (ColumnExists(con, factvTable, "TIP_DOC_SIG", tx)) sets.Add("TIP_DOC_SIG = @TIPSIG");
            if (ColumnExists(con, factvTable, "DOC_SIG", tx)) sets.Add("DOC_SIG = @DOCSIG");
            if (ColumnExists(con, factvTable, "ENLAZADO", tx)) sets.Add("ENLAZADO = 'S'");

            if (sets.Count == 0)
                return;

            using var cmd = new FbCommand($"UPDATE {factvTable} SET {string.Join(", ", sets)} WHERE CVE_DOC = @DOC", con, tx);
            if (sets.Any(s => s.Contains("@TIPSIG"))) cmd.Parameters.Add("@TIPSIG", FbDbType.VarChar).Value = tipDocSig;
            if (sets.Any(s => s.Contains("@DOCSIG"))) cmd.Parameters.Add("@DOCSIG", FbDbType.VarChar).Value = docSig;
            cmd.Parameters.Add("@DOC", FbDbType.VarChar).Value = notaDoc;
            cmd.ExecuteNonQuery();
        }

        private static void InsertDocumentCrossLinks(
            FbConnection con,
            FbTransaction tx,
            string table,
            string notaDoc,
            string facturaDoc,
            IEnumerable<DocumentLineWriteRow> lines)
        {
            string nota = notaDoc;
            string factura = facturaDoc;

            using (var del = new FbCommand($@"DELETE FROM {table} WHERE (TIP_DOC = 'V' AND CVE_DOC = @NV) OR (TIP_DOC = 'F' AND CVE_DOC = @FA)", con, tx))
            {
                del.Parameters.Add("@NV", FbDbType.VarChar).Value = nota;
                del.Parameters.Add("@FA", FbDbType.VarChar).Value = factura;
                del.ExecuteNonQuery();
            }

            using var cmd = new FbCommand($@"
INSERT INTO {table}
(TIP_DOC, CVE_DOC, ANT_SIG, TIP_DOC_E, CVE_DOC_E, PARTIDA, PART_E, CANT_E)
VALUES (@TIP_DOC, @CVE_DOC, @ANT_SIG, @TIP_DOC_E, @CVE_DOC_E, @PARTIDA, @PART_E, @CANT_E)", con, tx);

            var pTipDoc = cmd.Parameters.Add("@TIP_DOC", FbDbType.VarChar);
            var pCveDoc = cmd.Parameters.Add("@CVE_DOC", FbDbType.VarChar);
            var pAntSig = cmd.Parameters.Add("@ANT_SIG", FbDbType.VarChar);
            var pTipDocE = cmd.Parameters.Add("@TIP_DOC_E", FbDbType.VarChar);
            var pCveDocE = cmd.Parameters.Add("@CVE_DOC_E", FbDbType.VarChar);
            var pPartida = cmd.Parameters.Add("@PARTIDA", FbDbType.Integer);
            var pPartE = cmd.Parameters.Add("@PART_E", FbDbType.Integer);
            var pCantE = cmd.Parameters.Add("@CANT_E", FbDbType.Double);

            foreach (var row in lines.OrderBy(x => x.NumPar))
            {
                var qty = row.Cantidad <= 0m ? 0d : Convert.ToDouble(row.Cantidad);

                pTipDoc.Value = "V";
                pCveDoc.Value = nota;
                pAntSig.Value = "S";
                pTipDocE.Value = "F";
                pCveDocE.Value = factura;
                pPartida.Value = row.NumPar;
                pPartE.Value = row.NumPar;
                pCantE.Value = qty;
                cmd.ExecuteNonQuery();

                pTipDoc.Value = "F";
                pCveDoc.Value = factura;
                pAntSig.Value = "A";
                pTipDocE.Value = "V";
                pCveDocE.Value = nota;
                pPartida.Value = row.NumPar;
                pPartE.Value = row.NumPar;
                pCantE.Value = qty;
                cmd.ExecuteNonQuery();
            }
        }

        private static SaeClientInfo EnsureMostradorClient(FbConnection con, FbTransaction tx, string clieTable)
        {
            var info = new SaeClientInfo
            {
                Clave = Truncate(GetConfigOrDefault(CfgClienteMostradorClave, "MOSTR"), 10).ToUpperInvariant(),
                Nombre = Truncate(GetConfigOrDefault(CfgClienteMostradorNombre, "MOSTR"), 254),
                Rfc = Truncate(GetConfigOrDefault(CfgClienteMostradorRfc, "XAXX010101000"), 15).ToUpperInvariant(),
                UsoCfdi = Truncate(GetConfigOrDefault(CfgClienteMostradorUsoCfdi, "S01"), 5),
                MetodoPago = Truncate(GetConfigOrDefault(CfgClienteMostradorMetodoPago, "PUE"), 255),
                FormaPagoSat = Truncate(GetConfigOrDefault(CfgClienteMostradorFormaPago, "01"), 5),
                RegFiscal = NullIfWhiteSpace(Truncate(GetConfigOrDefault(CfgClienteMostradorRegFiscal, string.Empty), 4)),
                CodigoPostal = null,
                Pais = "MEX",
                CvePaisSat = "MEX",
                ListaPrecios = ParseIntOrNull(AuxDbInitializer.GetConfig("LISTA_PRECIOS")) ?? 1
            };

            UpsertClient(con, tx, clieTable, info);
            return info;
        }

        private static int ReadNextBitacoraCounter(FbConnection con, FbTransaction tx, string factvTable, string? factfTable)
        {
            var tables = new List<string> { factvTable };
            if (!string.IsNullOrWhiteSpace(factfTable))
                tables.Add(factfTable!);

            int maxVal = 0;
            foreach (var table in tables)
            {
                using var cmd = new FbCommand($@"
SELECT MAX(COALESCE(CVE_BITA, 0))
FROM {table}", con, tx);
                var val = cmd.ExecuteScalar();
                if (val != null && val != DBNull.Value)
                    maxVal = Math.Max(maxVal, Convert.ToInt32(val));
            }
            return maxVal + 1;
        }

        private static SaeClientInfo EnsureFacturaClient(FbConnection con, FbTransaction tx, string clieTable, CobroRequest request)
        {
            var rfc = (request.Rfc ?? string.Empty).Trim().ToUpperInvariant();
            var nombre = (request.RazonSocial ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(rfc) || string.IsNullOrWhiteSpace(nombre))
                throw new InvalidOperationException("Para facturar se requiere RFC y Razón social.");

            var existingKey = ResolveClientKeyForFactura(con, tx, clieTable, request.ClienteClaveSae, rfc);
            var info = new SaeClientInfo
            {
                Clave = existingKey ?? GenerateClientKey(con, tx, clieTable, rfc),
                Nombre = Truncate(nombre, 254),
                Rfc = Truncate(rfc, 15),
                UsoCfdi = Truncate(FirstNonEmpty(request.UsoCfdi, "G03"), 5),
                MetodoPago = Truncate(FirstNonEmpty(request.MetodoPago, "PUE"), 255),
                FormaPagoSat = Truncate(FirstNonEmpty(request.FormaPago, "99"), 5),
                RegFiscal = NullIfWhiteSpace(Truncate(request.RegFiscal ?? string.Empty, 4)),
                CodigoPostal = NullIfWhiteSpace(Truncate(request.CodigoPostal ?? string.Empty, 5)),
                Pais = "MEX",
                CvePaisSat = "MEX",
                ListaPrecios = ParseIntOrNull(AuxDbInitializer.GetConfig("LISTA_PRECIOS")) ?? 1
            };

            UpsertClient(con, tx, clieTable, info);
            return info;
        }

        private static string? ResolveClientKeyForFactura(FbConnection con, FbTransaction tx, string clieTable, string? requestedKey, string rfc)
        {
            if (!string.IsNullOrWhiteSpace(requestedKey))
            {
                var trimmed = requestedKey.Trim().ToUpperInvariant();
                if (ClientExistsByKey(con, tx, clieTable, trimmed))
                    return trimmed;
            }

            if (!ColumnExists(con, clieTable, "RFC", tx))
                return null;

            using var cmd = new FbCommand($@"
SELECT FIRST 1 CLAVE
FROM {clieTable}
WHERE UPPER(COALESCE(RFC, '')) = @RFC", con, tx);
            cmd.Parameters.Add("@RFC", FbDbType.VarChar).Value = rfc;
            var value = cmd.ExecuteScalar();
            return value == null || value == DBNull.Value ? null : value.ToString()?.Trim();
        }

        private static string GenerateClientKey(FbConnection con, FbTransaction tx, string clieTable, string rfc)
        {
            string baseKey = new string(rfc.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(baseKey))
                baseKey = "CLIENTE";
            baseKey = baseKey.Length > 10 ? baseKey[..10] : baseKey;

            if (!ClientExistsByKey(con, tx, clieTable, baseKey))
                return baseKey;

            var prefix = baseKey.Length >= 8 ? baseKey[..8] : baseKey;
            for (int i = 1; i <= 99; i++)
            {
                var candidate = (prefix + i.ToString("00")).ToUpperInvariant();
                candidate = candidate.Length > 10 ? candidate[..10] : candidate;
                if (!ClientExistsByKey(con, tx, clieTable, candidate))
                    return candidate;
            }

            throw new InvalidOperationException("No fue posible generar una clave única de cliente en SAE.");
        }

        private static bool ClientExistsByKey(FbConnection con, FbTransaction tx, string clieTable, string key)
        {
            using var cmd = new FbCommand($"SELECT COUNT(*) FROM {clieTable} WHERE CLAVE = @K", con, tx);
            cmd.Parameters.Add("@K", FbDbType.VarChar).Value = key;
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        private static void UpsertClient(FbConnection con, FbTransaction tx, string clieTable, SaeClientInfo info)
        {
            if (ClientExistsByKey(con, tx, clieTable, info.Clave))
            {
                var sets = new Dictionary<string, object?>
                {
                    ["STATUS"] = "A",
                    ["NOMBRE"] = info.Nombre,
                    ["RFC"] = info.Rfc,
                    ["USO_CFDI"] = info.UsoCfdi,
                    ["METODODEPAGO"] = info.MetodoPago,
                    ["FORMADEPAGOSAT"] = info.FormaPagoSat,
                    ["REG_FISC"] = info.RegFiscal,
                    ["CODIGO"] = info.CodigoPostal,
                    ["PAIS"] = info.Pais,
                    ["CVE_PAIS_SAT"] = info.CvePaisSat,
                    ["LISTA_PREC"] = info.ListaPrecios,
                    ["IMPRIR"] = "S"
                };
                UpdateDynamicByKey(con, tx, clieTable, "CLAVE", info.Clave, sets);
                return;
            }

            var map = new Dictionary<string, object?>
            {
                ["CLAVE"] = info.Clave,
                ["STATUS"] = "A",
                ["NOMBRE"] = info.Nombre,
                ["RFC"] = info.Rfc,
                ["USO_CFDI"] = info.UsoCfdi,
                ["METODODEPAGO"] = info.MetodoPago,
                ["FORMADEPAGOSAT"] = info.FormaPagoSat,
                ["REG_FISC"] = info.RegFiscal,
                ["CODIGO"] = info.CodigoPostal,
                ["PAIS"] = info.Pais,
                ["CVE_PAIS_SAT"] = info.CvePaisSat,
                ["LISTA_PREC"] = info.ListaPrecios,
                ["VAL_RFC"] = 0,
                ["IMPRIR"] = "S",
                ["MAIL"] = "N",
                ["CON_CREDITO"] = "N",
                ["PROSPECTO"] = "N"
            };
            InsertDynamic(con, tx, clieTable, map);
        }

        private static SaeDocumentNumber ReserveNextDocumentNumber(FbConnection con, FbTransaction tx, string foliosfTable, string paramFoliosfTable, string tipDoc, string preferredSeries)
        {
            var folioRow = ReadFolioRow(con, tx, foliosfTable, tipDoc, preferredSeries)
                           ?? ReadFolioRow(con, tx, foliosfTable, tipDoc, null)
                           ?? throw new InvalidOperationException($"No encontré una serie activa para el documento {tipDoc}.");

            int longitud = ReadDocLength(con, tx, paramFoliosfTable, tipDoc, folioRow.Serie);
            int next = Math.Max(folioRow.UltDoc + 1, folioRow.FolioDesde);
            if (folioRow.FolioHasta.HasValue && next > folioRow.FolioHasta.Value)
                throw new InvalidOperationException($"La serie {folioRow.Serie} para {tipDoc} ya agotó su rango de folios.");

            using (var upd = new FbCommand($@"
UPDATE {foliosfTable}
SET ULT_DOC = @ULT,
    FECH_ULT_DOC = @F
WHERE TIP_DOC = @T
  AND TRIM(SERIE) = @S
  AND FOLIODESDE = @FD", con, tx))
            {
                upd.Parameters.Add("@ULT", FbDbType.Integer).Value = next;
                upd.Parameters.Add("@F", FbDbType.TimeStamp).Value = DateTime.Now;
                upd.Parameters.Add("@T", FbDbType.VarChar).Value = tipDoc;
                upd.Parameters.Add("@S", FbDbType.VarChar).Value = folioRow.Serie;
                upd.Parameters.Add("@FD", FbDbType.Integer).Value = folioRow.FolioDesde;
                upd.ExecuteNonQuery();
            }

            string folioTexto = next.ToString().PadLeft(longitud, '0');
            string cveDoc = new string(' ', 10) + folioTexto;

            return new SaeDocumentNumber
            {
                TipDoc = tipDoc,
                Serie = folioRow.Serie,
                Folio = next,
                Longitud = longitud,
                CveDoc = cveDoc
            };
        }

        private sealed class FolioRow
        {
            public string Serie { get; set; } = string.Empty;
            public int FolioDesde { get; set; }
            public int? FolioHasta { get; set; }
            public int UltDoc { get; set; }
        }

        private static FolioRow? ReadFolioRow(FbConnection con, FbTransaction tx, string table, string tipDoc, string? preferredSeries)
        {
            var sql = new StringBuilder($@"
SELECT FIRST 1 TRIM(SERIE), FOLIODESDE, FOLIOHASTA, COALESCE(ULT_DOC, 0)
FROM {table}
WHERE TIP_DOC = @T
  AND COALESCE(STATUS, 'D') <> 'B'");
            if (!string.IsNullOrWhiteSpace(preferredSeries))
                sql.Append(" AND TRIM(SERIE) = @S");
            sql.Append(" ORDER BY FOLIODESDE");

            using var cmd = new FbCommand(sql.ToString(), con, tx);
            cmd.Parameters.Add("@T", FbDbType.VarChar).Value = tipDoc;
            if (!string.IsNullOrWhiteSpace(preferredSeries))
                cmd.Parameters.Add("@S", FbDbType.VarChar).Value = preferredSeries!.Trim();

            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return null;

            int? folioHasta = null;
            if (!rd.IsDBNull(2))
            {
                var rawHasta = Convert.ToInt32(rd.GetValue(2));
                if (rawHasta > 0)
                    folioHasta = rawHasta;
            }

            return new FolioRow
            {
                Serie = rd.IsDBNull(0) ? string.Empty : rd.GetString(0).Trim(),
                FolioDesde = rd.IsDBNull(1) ? 1 : Convert.ToInt32(rd.GetValue(1)),
                FolioHasta = folioHasta,
                UltDoc = rd.IsDBNull(3) ? 0 : Convert.ToInt32(rd.GetValue(3))
            };
        }

        private static int ReadDocLength(FbConnection con, FbTransaction tx, string table, string tipDoc, string serie)
        {
            using var cmd = new FbCommand($@"
SELECT FIRST 1 COALESCE(LONGITUD, 10)
FROM {table}
WHERE TIPODOCTO = @T
  AND TRIM(SERIE) = @S", con, tx);
            cmd.Parameters.Add("@T", FbDbType.VarChar).Value = tipDoc;
            cmd.Parameters.Add("@S", FbDbType.VarChar).Value = serie.Trim();
            var value = cmd.ExecuteScalar();
            if (value == null || value == DBNull.Value)
                return 10;
            return Convert.ToInt32(value);
        }

        private static SaeArticleInfo ReadArticleInfo(FbConnection con, FbTransaction tx, string inveTable, string cveArt, bool esPlatillo)
        {
            var info = new SaeArticleInfo
            {
                Clave = cveArt,
                Descripcion = cveArt,
                UnidadVenta = esPlatillo ? "pz" : "pz",
                Costo = 0m,
                TipoProd = esPlatillo ? "K" : "P",
                TipoElem = esPlatillo ? "K" : "P"
            };

            var selectParts = new List<string> { "TRIM(CVE_ART)" };
            if (ColumnExists(con, inveTable, "DESCR", tx)) selectParts.Add("DESCR"); else selectParts.Add("NULL");
            if (ColumnExists(con, inveTable, "UNI_MED", tx)) selectParts.Add("UNI_MED"); else selectParts.Add("NULL");
            if (ColumnExists(con, inveTable, "COSTO_PROM", tx)) selectParts.Add("COSTO_PROM"); else selectParts.Add("0");
            if (ColumnExists(con, inveTable, "CVE_PRODSERV", tx)) selectParts.Add("CVE_PRODSERV"); else selectParts.Add("NULL");
            if (ColumnExists(con, inveTable, "CVE_UNIDAD", tx)) selectParts.Add("CVE_UNIDAD"); else selectParts.Add("NULL");
            if (ColumnExists(con, inveTable, "CVE_ESQIMPU", tx)) selectParts.Add("CVE_ESQIMPU"); else selectParts.Add("1");

            using var cmd = new FbCommand($@"
SELECT {string.Join(", ", selectParts)}
FROM {inveTable}
WHERE CVE_ART = @C", con, tx);
            cmd.Parameters.Add("@C", FbDbType.VarChar).Value = cveArt;
            using var rd = cmd.ExecuteReader();
            if (!rd.Read())
                return info;

            info.Descripcion = rd.IsDBNull(1) ? cveArt : rd.GetString(1).Trim();
            info.UnidadVenta = rd.IsDBNull(2) ? (esPlatillo ? "pz" : "pz") : rd.GetString(2).Trim();
            info.Costo = rd.IsDBNull(3) ? 0m : Convert.ToDecimal(rd.GetValue(3));
            info.CveProdServ = rd.IsDBNull(4) ? null : rd.GetString(4).Trim();
            info.CveUnidad = rd.IsDBNull(5) ? null : rd.GetString(5).Trim();
            info.CveEsqImpu = rd.IsDBNull(6) ? null : Convert.ToInt32(rd.GetValue(6));
            return info;
        }

        private static decimal GetDocumentQuantity(CobroLineaSnapshot linea)
        {
            if (linea.PesoGr.HasValue && linea.PesoGr.Value > 0m)
                return Math.Round(linea.PesoGr.Value / 1000m, 6, MidpointRounding.AwayFromZero);

            return Math.Round(linea.Cantidad, 6, MidpointRounding.AwayFromZero);
        }

        private static void InsertDynamic(FbConnection con, FbTransaction tx, string table, IDictionary<string, object?> values)
        {
            var cols = new List<string>();
            var pars = new List<string>();
            var items = values
                .Where(kv => kv.Value != null && ColumnExists(con, table, kv.Key, tx))
                .ToList();

            if (items.Count == 0)
                throw new InvalidOperationException($"No hubo columnas válidas para insertar en {table}.");

            using var cmd = new FbCommand { Connection = con, Transaction = tx };
            int i = 0;
            foreach (var item in items)
            {
                string p = "@P" + i++;
                cols.Add(item.Key);
                pars.Add(p);
                cmd.Parameters.Add(new FbParameter(p, item.Value ?? DBNull.Value));
            }

            cmd.CommandText = $"INSERT INTO {table} ({string.Join(", ", cols)}) VALUES ({string.Join(", ", pars)})";
            cmd.ExecuteNonQuery();
        }

        private static void UpdateDynamicByKey(FbConnection con, FbTransaction tx, string table, string keyColumn, object keyValue, IDictionary<string, object?> values)
        {
            var items = values
                .Where(kv => ColumnExists(con, table, kv.Key, tx))
                .ToList();
            if (items.Count == 0)
                return;

            using var cmd = new FbCommand { Connection = con, Transaction = tx };
            var sets = new List<string>();
            int i = 0;
            foreach (var item in items)
            {
                string p = "@P" + i++;
                sets.Add($"{item.Key} = {p}");
                cmd.Parameters.Add(new FbParameter(p, item.Value ?? DBNull.Value));
            }
            cmd.Parameters.Add(new FbParameter("@KEY", keyValue));
            cmd.CommandText = $"UPDATE {table} SET {string.Join(", ", sets)} WHERE {keyColumn} = @KEY";
            cmd.ExecuteNonQuery();
        }

        private static void ValidateColumns(FbConnection con, string table, SaeCobroValidationResult result, bool isWarning, params string[] columns)
        {
            if (string.IsNullOrWhiteSpace(table))
                return;

            foreach (var col in columns)
            {
                if (ColumnExists(con, table, col))
                    continue;

                var msg = $"La tabla {table} no contiene la columna {col}.";
                if (isWarning) result.Warnings.Add(msg);
                else result.Errors.Add(msg);
            }
        }

        private static int ParseWarehouseNumberOrThrow(string? warehouseText)
        {
            var parsed = ParseWarehouseNumber(warehouseText ?? string.Empty);
            if (!parsed.HasValue)
                throw new InvalidOperationException($"El almacén configurado '{warehouseText}' no tiene un número válido.");
            return parsed.Value;
        }

        private static int? ParseWarehouseNumber(string warehouseText)
        {
            if (int.TryParse(warehouseText.Trim(), out var direct))
                return direct;

            var digits = new string(warehouseText.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var parsed))
                return parsed;

            return null;
        }

        private static int? ParseIntOrNull(string? value)
        {
            return int.TryParse((value ?? string.Empty).Trim(), out var parsed) ? parsed : null;
        }

        private static string? NullIfWhiteSpace(string? value)
            => string.IsNullOrWhiteSpace(value) ? null : value;

        private static string Truncate(string value, int maxLen)
        {
            if (string.IsNullOrEmpty(value))
                return value;
            return value.Length <= maxLen ? value : value[..maxLen];
        }

        private static string FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
                if (!string.IsNullOrWhiteSpace(value))
                    return value!.Trim();
            return string.Empty;
        }

        private static string GetRequiredTable(FbConnection con, string baseName, SaeCobroValidationResult result)
        {
            var table = GetTableIfExists(con, baseName);
            if (string.IsNullOrWhiteSpace(table))
                result.Errors.Add($"No encontré la tabla {baseName}## en la BD de SAE.");
            return table;
        }

        private static string GetOptionalTable(FbConnection con, string baseName, SaeCobroValidationResult result)
        {
            var table = GetTableIfExists(con, baseName);
            if (string.IsNullOrWhiteSpace(table))
                result.Warnings.Add($"No encontré la tabla opcional {baseName}##. Esa parte del flujo final deberá ajustarse en tu SAE real.");
            return table;
        }

        private static string GetTableIfExists(FbConnection con, string baseName)
        {
            try
            {
                return SaeDb.GetTableName(con, baseName);
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool ColumnExists(FbConnection con, string table, string column, FbTransaction? tx = null)
        {
            if (string.IsNullOrWhiteSpace(table) || string.IsNullOrWhiteSpace(column))
                return false;

            const string sql = @"
SELECT COUNT(*)
FROM RDB$RELATION_FIELDS
WHERE TRIM(RDB$RELATION_NAME) = @T
  AND TRIM(RDB$FIELD_NAME) = @C";

            using var cmd = tx == null
                ? new FbCommand(sql, con)
                : new FbCommand(sql, con, tx);
            cmd.Parameters.Add("@T", FbDbType.VarChar).Value = table.Trim().ToUpperInvariant();
            cmd.Parameters.Add("@C", FbDbType.VarChar).Value = column.Trim().ToUpperInvariant();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }
    }
}
