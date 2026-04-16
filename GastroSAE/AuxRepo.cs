using FirebirdSql.Data.FirebirdClient;

namespace GastroSAE
{
    public static class AuxRepo
    {
        public class MesaDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public int? Capacidad { get; set; }
            public string Estado { get; set; } = string.Empty;
            public int? MeseroIdActual { get; set; }
            public string? MeseroNombre { get; set; }
        }

        public class MeseroDto
        {
            public int Id { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public bool Activo { get; set; }
        }

        public class MesaCobroDto
        {
            public int IdMesa { get; set; }
            public string NombreMesa { get; set; } = string.Empty;
            public string EstadoMesa { get; set; } = string.Empty;
            public int IdPedido { get; set; }
        }

        public class PedidoDetDto
        {
            public int IdDet { get; set; }
            public string ClaveArticulo { get; set; } = string.Empty;
            public bool EsPlatillo { get; set; }
            public decimal Cantidad { get; set; }
            public decimal? PesoGr { get; set; }
            public decimal PrecioUnit { get; set; }
            public decimal Importe { get; set; }
        }

        public class TurnoInfo
        {
            public int Id { get; set; }
            public DateTime Fecha { get; set; }
            public TimeSpan HoraIni { get; set; }
            public TimeSpan? HoraFin { get; set; }
            public string Responsable { get; set; } = string.Empty;
            public string Obs { get; set; } = string.Empty;
        }

        public sealed class AbrirMesaResult
        {
            public int IdTurno { get; set; }
            public int IdMesaTurno { get; set; }
            public int IdPedido { get; set; }
        }

        public class CobroResult
        {
            public decimal Total { get; set; }
            public decimal Pagado { get; set; }
            public decimal Cambio { get; set; }
            public int IdMesa { get; set; }
            public int IdMesaTurno { get; set; }
        }

        private sealed class SessionMesaTurno
        {
            public int IdMesaTurno { get; set; }
            public int IdTurno { get; set; }
            public int IdMesa { get; set; }
            public int IdMesero { get; set; }
            public string Estado { get; set; } = "OCUPADA";
        }

        private sealed class SessionPedido
        {
            public int IdPedido { get; set; }
            public int IdMesaTurno { get; set; }
            public DateTime FechaHora { get; set; } = DateTime.Now;
            public string Estado { get; set; } = "ABIERTO";
            public List<PedidoDetDto> Detalles { get; } = new();
            public decimal Subtotal { get; set; }
            public decimal Impuesto { get; set; }
            public decimal Total { get; set; }
        }

        private static readonly object _sync = new();
        private static TurnoInfo? _turnoAbierto;
        private static int _nextTurnoId = 1;
        private static int _nextMesaTurnoId = 1;
        private static int _nextPedidoId = 1;
        private static int _nextPedidoDetId = 1;
        private static readonly Dictionary<int, SessionMesaTurno> _mesaTurnoPorMesa = new();
        private static readonly Dictionary<int, SessionPedido> _pedidosPorId = new();


        public static void InicializarSesionTemporal()
        {
            lock (_sync)
            {
                _turnoAbierto = null;
                _mesaTurnoPorMesa.Clear();
                _pedidosPorId.Clear();
                _nextMesaTurnoId = 1;
                _nextPedidoId = 1;
                _nextPedidoDetId = 1;
            }

            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand("UPDATE MESAS SET ESTADO='LIBRE' WHERE ESTADO <> 'LIBRE' OR ESTADO IS NULL", conn);
            cmd.ExecuteNonQuery();
        }

        // ===== MESAS (persistentes) =====
        public static List<MesaDto> ListarMesas()
        {
            var list = new List<MesaDto>();
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand("SELECT ID_MESA, NOMBRE, CAPACIDAD, ESTADO FROM MESAS ORDER BY ID_MESA", conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                var idMesa = Convert.ToInt32(rd[0]);
                var dto = new MesaDto
                {
                    Id = idMesa,
                    Nombre = rd[1]?.ToString() ?? string.Empty,
                    Capacidad = rd[2] == DBNull.Value ? (int?)null : Convert.ToInt32(rd[2]),
                    Estado = rd[3]?.ToString() ?? "LIBRE"
                };

                lock (_sync)
                {
                    if (_mesaTurnoPorMesa.TryGetValue(idMesa, out var mt))
                    {
                        dto.Estado = mt.Estado;
                        dto.MeseroIdActual = mt.IdMesero;
                        dto.MeseroNombre = ObtenerNombreMesero(conn, mt.IdMesero);
                    }
                }

                list.Add(dto);
            }
            return list;
        }

        public static int InsertMesa(string nombre, int? capacidad)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido");
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var tx = conn.BeginTransaction();
            using var cmd = new FbCommand(@"INSERT INTO MESAS (NOMBRE, CAPACIDAD, ESTADO) VALUES (@N,@C,'LIBRE') RETURNING ID_MESA", conn, tx);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 30) { Value = nombre.Trim() });
            cmd.Parameters.Add(new FbParameter("@C", FbDbType.Integer) { Value = (object?)capacidad ?? DBNull.Value });
            int id = Convert.ToInt32(cmd.ExecuteScalar());
            tx.Commit();
            return id;
        }

        public static void UpdateMesa(int id, string nombre, int? capacidad)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"UPDATE MESAS SET NOMBRE=@N, CAPACIDAD=@C WHERE ID_MESA=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 30) { Value = nombre?.Trim() ?? "" });
            cmd.Parameters.Add(new FbParameter("@C", FbDbType.Integer) { Value = (object?)capacidad ?? DBNull.Value });
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        public static void DeleteMesa(int id)
        {
            lock (_sync)
            {
                if (_mesaTurnoPorMesa.ContainsKey(id))
                    throw new InvalidOperationException("No puedes eliminar una mesa mientras está siendo atendida en la sesión actual.");
            }

            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"DELETE FROM MESAS WHERE ID_MESA=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        // ===== MESEROS (persistentes) =====
        public static List<MeseroDto> ListarMeseros(bool soloActivos = true)
        {
            var list = new List<MeseroDto>();
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            string sql = "SELECT ID_MESERO, NOMBRE, ACTIVO FROM MESEROS " + (soloActivos ? "WHERE ACTIVO=1 " : "") + "ORDER BY NOMBRE";
            using var cmd = new FbCommand(sql, conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new MeseroDto
                {
                    Id = Convert.ToInt32(rd[0]),
                    Nombre = rd[1]?.ToString() ?? string.Empty,
                    Activo = Convert.ToInt16(rd[2]) == 1
                });
            }
            return list;
        }

        public static int InsertMesero(string nombre, bool activo)
        {
            if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido");
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var tx = conn.BeginTransaction();
            using var cmd = new FbCommand(@"INSERT INTO MESEROS (NOMBRE, ACTIVO) VALUES (@N, @A) RETURNING ID_MESERO", conn, tx);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 60) { Value = nombre.Trim() });
            cmd.Parameters.Add(new FbParameter("@A", FbDbType.SmallInt) { Value = activo ? 1 : 0 });
            int id = Convert.ToInt32(cmd.ExecuteScalar());
            tx.Commit();
            return id;
        }

        public static void UpdateMesero(int id, string nombre, bool activo)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"UPDATE MESEROS SET NOMBRE=@N, ACTIVO=@A WHERE ID_MESERO=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@N", FbDbType.VarChar, 60) { Value = nombre?.Trim() ?? "" });
            cmd.Parameters.Add(new FbParameter("@A", FbDbType.SmallInt) { Value = activo ? 1 : 0 });
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        public static void DeleteMesero(int id)
        {
            lock (_sync)
            {
                if (_mesaTurnoPorMesa.Values.Any(x => x.IdMesero == id))
                    throw new InvalidOperationException("No puedes eliminar un mesero mientras atiende una mesa en la sesión actual.");
            }

            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand(@"DELETE FROM MESEROS WHERE ID_MESERO=@ID", conn);
            cmd.Parameters.Add(new FbParameter("@ID", FbDbType.Integer) { Value = id });
            cmd.ExecuteNonQuery();
        }

        // ===== TURNO (en memoria) =====
        public static int GetOrOpenTurnoDelDia()
        {
            lock (_sync)
            {
                if (_turnoAbierto != null)
                    return _turnoAbierto.Id;

                _turnoAbierto = new TurnoInfo
                {
                    Id = _nextTurnoId++,
                    Fecha = DateTime.Today,
                    HoraIni = DateTime.Now.TimeOfDay,
                    Responsable = "SISTEMA",
                    Obs = string.Empty
                };
                return _turnoAbierto.Id;
            }
        }

        public static int? GetTurnoAbiertoId()
        {
            lock (_sync)
                return _turnoAbierto?.Id;
        }

        public static TurnoInfo? ObtenerTurnoAbiertoInfo()
        {
            lock (_sync)
            {
                if (_turnoAbierto == null) return null;
                return new TurnoInfo
                {
                    Id = _turnoAbierto.Id,
                    Fecha = _turnoAbierto.Fecha,
                    HoraIni = _turnoAbierto.HoraIni,
                    HoraFin = _turnoAbierto.HoraFin,
                    Responsable = _turnoAbierto.Responsable,
                    Obs = _turnoAbierto.Obs
                };
            }
        }

        public static int AbrirTurno(string responsable, string obs)
        {
            if (string.IsNullOrWhiteSpace(responsable))
                throw new ArgumentException("Captura el responsable del turno.");

            lock (_sync)
            {
                if (_turnoAbierto != null)
                    return _turnoAbierto.Id;

                _turnoAbierto = new TurnoInfo
                {
                    Id = _nextTurnoId++,
                    Fecha = DateTime.Today,
                    HoraIni = DateTime.Now.TimeOfDay,
                    Responsable = responsable.Trim(),
                    Obs = obs?.Trim() ?? string.Empty
                };
                return _turnoAbierto.Id;
            }
        }

        public static bool HayMesasAbiertas(int idTurno)
        {
            lock (_sync)
                return _turnoAbierto?.Id == idTurno && _mesaTurnoPorMesa.Values.Any(x => x.Estado is "OCUPADA" or "EN_CUENTA");
        }

        public static void CerrarTurno(int idTurno)
        {
            lock (_sync)
            {
                if (_turnoAbierto == null || _turnoAbierto.Id != idTurno)
                    return;

                if (_mesaTurnoPorMesa.Values.Any(x => x.Estado is "OCUPADA" or "EN_CUENTA"))
                    throw new InvalidOperationException("No puedes cerrar el turno: hay mesas abiertas en la sesión actual.");

                _turnoAbierto.HoraFin = DateTime.Now.TimeOfDay;
                _turnoAbierto = null;
            }
        }

        public static bool ExistenMesasOcupadasOEnCuenta()
        {
            lock (_sync)
                return _mesaTurnoPorMesa.Values.Any(x => x.Estado is "OCUPADA" or "EN_CUENTA");
        }

        // ===== OPERACIÓN DE MESAS Y PEDIDOS (solo memoria de sesión) =====
        public static AbrirMesaResult AbrirMesa(int idMesa, int idMesero)
        {
            int idTurno = GetOrOpenTurnoDelDia();

            lock (_sync)
            {
                if (_mesaTurnoPorMesa.TryGetValue(idMesa, out var existente) && existente.Estado is "OCUPADA" or "EN_CUENTA")
                    throw new InvalidOperationException("La mesa ya está abierta en la sesión actual.");

                string path;
                using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
                using (var cmdMesa = new FbCommand("SELECT ESTADO FROM MESAS WHERE ID_MESA=@M", conn))
                {
                    cmdMesa.Parameters.Add("@M", FbDbType.Integer).Value = idMesa;
                    var estado = cmdMesa.ExecuteScalar()?.ToString() ?? "LIBRE";
                    if (!string.Equals(estado, "LIBRE", StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("La mesa no está LIBRE.");
                }

                var mesaTurno = new SessionMesaTurno
                {
                    IdMesaTurno = _nextMesaTurnoId++,
                    IdTurno = idTurno,
                    IdMesa = idMesa,
                    IdMesero = idMesero,
                    Estado = "OCUPADA"
                };
                var pedido = new SessionPedido
                {
                    IdPedido = _nextPedidoId++,
                    IdMesaTurno = mesaTurno.IdMesaTurno,
                    FechaHora = DateTime.Now,
                    Estado = "ABIERTO"
                };

                _mesaTurnoPorMesa[idMesa] = mesaTurno;
                _pedidosPorId[pedido.IdPedido] = pedido;
                ActualizarEstadoMesaPersistente(idMesa, "OCUPADA");

                return new AbrirMesaResult
                {
                    IdTurno = idTurno,
                    IdMesaTurno = mesaTurno.IdMesaTurno,
                    IdPedido = pedido.IdPedido
                };
            }
        }

        public static List<MesaCobroDto> ListarMesasConPedidoAbierto()
        {
            lock (_sync)
            {
                var nombresMesa = ObtenerMesasPersistidas();
                return _pedidosPorId.Values
                    .Where(p => string.Equals(p.Estado, "ABIERTO", StringComparison.OrdinalIgnoreCase))
                    .Select(p =>
                    {
                        var mt = _mesaTurnoPorMesa.Values.FirstOrDefault(x => x.IdMesaTurno == p.IdMesaTurno);
                        if (mt == null) return null;
                        nombresMesa.TryGetValue(mt.IdMesa, out var nombreMesa);
                        return new MesaCobroDto
                        {
                            IdMesa = mt.IdMesa,
                            NombreMesa = nombreMesa ?? $"Mesa {mt.IdMesa}",
                            EstadoMesa = mt.Estado,
                            IdPedido = p.IdPedido
                        };
                    })
                    .Where(x => x != null)
                    .OrderBy(x => x!.NombreMesa)
                    .Cast<MesaCobroDto>()
                    .ToList();
            }
        }

        public static void LiberarMesa(int idMesa)
        {
            lock (_sync)
            {
                if (_mesaTurnoPorMesa.TryGetValue(idMesa, out var mt))
                {
                    foreach (var pedido in _pedidosPorId.Values.Where(x => x.IdMesaTurno == mt.IdMesaTurno).ToList())
                        _pedidosPorId.Remove(pedido.IdPedido);

                    _mesaTurnoPorMesa.Remove(idMesa);
                }
            }

            ActualizarEstadoMesaPersistente(idMesa, "LIBRE");
        }

        public static (int? IdPedido, int? IdMesaTurno) ObtenerPedidoAbiertoPorMesa(int idMesa)
        {
            lock (_sync)
            {
                if (!_mesaTurnoPorMesa.TryGetValue(idMesa, out var mt))
                    return (null, null);

                var pedido = _pedidosPorId.Values.FirstOrDefault(x => x.IdMesaTurno == mt.IdMesaTurno && x.Estado == "ABIERTO");
                if (pedido == null) return (null, null);
                return (pedido.IdPedido, mt.IdMesaTurno);
            }
        }

        public static int AgregarPedidoLinea(int idPedido, string cveArt, bool esPlatillo, decimal cantidad, decimal? pesoGr, decimal precioUnit)
        {
            lock (_sync)
            {
                var pedido = ObtenerPedidoOrThrow(idPedido);
                if (!string.Equals(pedido.Estado, "ABIERTO", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("El pedido ya no está abierto.");

                decimal importe = (pesoGr.HasValue && pesoGr.Value > 0m)
                    ? Math.Round((pesoGr.Value / 1000m) * precioUnit, 2)
                    : Math.Round(cantidad * precioUnit, 2);

                var det = new PedidoDetDto
                {
                    IdDet = _nextPedidoDetId++,
                    ClaveArticulo = cveArt,
                    EsPlatillo = esPlatillo,
                    Cantidad = cantidad,
                    PesoGr = pesoGr,
                    PrecioUnit = precioUnit,
                    Importe = importe
                };

                pedido.Detalles.Add(det);
                RecalcularTotalesPedidoInternal(pedido, 0.16m);
                return det.IdDet;
            }
        }

        public static List<PedidoDetDto> ListarPedidoDet(int idPedido)
        {
            lock (_sync)
            {
                var pedido = ObtenerPedidoOrThrow(idPedido);
                return pedido.Detalles
                    .Select(x => new PedidoDetDto
                    {
                        IdDet = x.IdDet,
                        ClaveArticulo = x.ClaveArticulo,
                        EsPlatillo = x.EsPlatillo,
                        Cantidad = x.Cantidad,
                        PesoGr = x.PesoGr,
                        PrecioUnit = x.PrecioUnit,
                        Importe = x.Importe
                    })
                    .ToList();
            }
        }

        public static Dictionary<string, decimal> ObtenerCantidadesPlatillosReservadas(int? excluirIdPedido = null)
        {
            lock (_sync)
            {
                return _pedidosPorId.Values
                    .Where(p => p.Estado == "ABIERTO" && (!excluirIdPedido.HasValue || p.IdPedido != excluirIdPedido.Value))
                    .SelectMany(p => p.Detalles.Where(d => d.EsPlatillo))
                    .GroupBy(d => d.ClaveArticulo, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.Sum(x => Math.Max(0m, x.Cantidad)), StringComparer.OrdinalIgnoreCase);
            }
        }

        public static void EliminarPedidoDet(int idPedidoDet)
        {
            lock (_sync)
            {
                foreach (var pedido in _pedidosPorId.Values)
                {
                    var det = pedido.Detalles.FirstOrDefault(x => x.IdDet == idPedidoDet);
                    if (det == null) continue;
                    pedido.Detalles.Remove(det);
                    RecalcularTotalesPedidoInternal(pedido, 0.16m);
                    return;
                }
            }
        }

        public static int ActualizarPrecioUnitArticuloEnPedido(int idPedido, string cveArt, decimal nuevoPrecioUnit)
        {
            lock (_sync)
            {
                var pedido = ObtenerPedidoOrThrow(idPedido);
                int rows = 0;
                foreach (var det in pedido.Detalles.Where(x => x.EsPlatillo && string.Equals(x.ClaveArticulo, cveArt, StringComparison.OrdinalIgnoreCase)))
                {
                    det.PrecioUnit = nuevoPrecioUnit;
                    det.Importe = (det.PesoGr.HasValue && det.PesoGr.Value > 0m)
                        ? Math.Round((det.PesoGr.Value / 1000m) * nuevoPrecioUnit, 2)
                        : Math.Round(det.Cantidad * nuevoPrecioUnit, 2);
                    rows++;
                }
                RecalcularTotalesPedidoInternal(pedido, 0.16m);
                return rows;
            }
        }

        public static (decimal Subtotal, decimal Impuesto, decimal Total) RecalcularTotalesPedido(int idPedido, decimal tasaIva = 0.16m)
        {
            lock (_sync)
            {
                var pedido = ObtenerPedidoOrThrow(idPedido);
                return RecalcularTotalesPedidoInternal(pedido, tasaIva);
            }
        }

        public static CobroSnapshot GetCobroSnapshot(int idPedido)
        {
            lock (_sync)
            {
                var pedido = ObtenerPedidoOrThrow(idPedido);
                var mt = _mesaTurnoPorMesa.Values.FirstOrDefault(x => x.IdMesaTurno == pedido.IdMesaTurno)
                    ?? throw new InvalidOperationException("Mesa del pedido no encontrada en la sesión actual.");

                return new CobroSnapshot
                {
                    IdPedido = pedido.IdPedido,
                    IdMesa = mt.IdMesa,
                    IdMesaTurno = mt.IdMesaTurno,
                    FechaHora = pedido.FechaHora,
                    EstadoPedido = pedido.Estado,
                    Subtotal = pedido.Subtotal,
                    Impuesto = pedido.Impuesto,
                    Total = pedido.Total,
                    Lineas = pedido.Detalles.Select(d => new CobroLineaSnapshot
                    {
                        IdPedidoDet = d.IdDet,
                        ClaveArticuloSae = d.ClaveArticulo,
                        EsPlatillo = d.EsPlatillo,
                        Cantidad = d.Cantidad,
                        PesoGr = d.PesoGr,
                        PrecioUnit = d.PrecioUnit,
                        Importe = d.Importe
                    }).ToList()
                };
            }
        }

        public static CobroResult CobrarPedido(int idPedido, decimal efectivo, decimal tarjeta, string referenciaTarjeta = null)
        {
            lock (_sync)
            {
                var pedido = ObtenerPedidoOrThrow(idPedido);
                var mt = _mesaTurnoPorMesa.Values.FirstOrDefault(x => x.IdMesaTurno == pedido.IdMesaTurno)
                    ?? throw new InvalidOperationException("Mesa del pedido no encontrada.");

                var (sub, imp, tot) = RecalcularTotalesPedidoInternal(pedido, 0.16m);
                decimal pagado = Math.Round(efectivo + tarjeta, 2);
                if (pagado < tot)
                    throw new InvalidOperationException($"Pago insuficiente. Total {tot:N2}, pagado {pagado:N2}.");

                var cambio = Math.Round(pagado - tot, 2);
                pedido.Estado = "COBRADO";
                mt.Estado = "CERRADA";

                _pedidosPorId.Remove(pedido.IdPedido);
                _mesaTurnoPorMesa.Remove(mt.IdMesa);
                ActualizarEstadoMesaPersistente(mt.IdMesa, "LIBRE");

                return new CobroResult
                {
                    Total = tot,
                    Pagado = pagado,
                    Cambio = cambio,
                    IdMesa = mt.IdMesa,
                    IdMesaTurno = mt.IdMesaTurno
                };
            }
        }

        // ===== helpers =====
        private static SessionPedido ObtenerPedidoOrThrow(int idPedido)
        {
            if (_pedidosPorId.TryGetValue(idPedido, out var pedido))
                return pedido;
            throw new InvalidOperationException("Pedido no encontrado en la sesión actual.");
        }

        private static (decimal Subtotal, decimal Impuesto, decimal Total) RecalcularTotalesPedidoInternal(SessionPedido pedido, decimal tasaIva)
        {
            var sub = Math.Round(pedido.Detalles.Sum(x => x.Importe), 2);
            var imp = Math.Round(sub * tasaIva, 2);
            var tot = Math.Round(sub + imp, 2);
            pedido.Subtotal = sub;
            pedido.Impuesto = imp;
            pedido.Total = tot;
            return (sub, imp, tot);
        }

        private static void ActualizarEstadoMesaPersistente(int idMesa, string estado)
        {
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand("UPDATE MESAS SET ESTADO=@E WHERE ID_MESA=@M", conn);
            cmd.Parameters.Add("@E", FbDbType.VarChar, 12).Value = estado;
            cmd.Parameters.Add("@M", FbDbType.Integer).Value = idMesa;
            cmd.ExecuteNonQuery();
        }

        private static string? ObtenerNombreMesero(FbConnection conn, int idMesero)
        {
            using var cmd = new FbCommand("SELECT NOMBRE FROM MESEROS WHERE ID_MESERO=@ID", conn);
            cmd.Parameters.Add("@ID", FbDbType.Integer).Value = idMesero;
            var o = cmd.ExecuteScalar();
            return o == null || o == DBNull.Value ? null : o.ToString();
        }

        private static Dictionary<int, string> ObtenerMesasPersistidas()
        {
            var map = new Dictionary<int, string>();
            string path;
            using var conn = AuxDbInitializer.EnsureCreated(out path, charset: "ISO8859_1");
            using var cmd = new FbCommand("SELECT ID_MESA, NOMBRE FROM MESAS", conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
                map[Convert.ToInt32(rd[0])] = rd[1]?.ToString() ?? string.Empty;
            return map;
        }
    }
}
