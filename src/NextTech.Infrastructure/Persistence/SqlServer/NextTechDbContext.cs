using Microsoft.EntityFrameworkCore;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer;

/// <summary>
/// DbContext de NextTechCustomDB.
/// La DB se creó primero mediante el script SQL oficial; no usar EnsureCreated().
/// </summary>
public sealed class NextTechDbContext(DbContextOptions<NextTechDbContext> options)
    : DbContext(options)
{
    public DbSet<Rol> Rol => Set<Rol>();
    public DbSet<EstadoOrden> EstadoOrden => Set<EstadoOrden>();
    public DbSet<EstadoCarrito> EstadoCarrito => Set<EstadoCarrito>();
    public DbSet<MetodoPago> MetodoPago => Set<MetodoPago>();
    public DbSet<EstadoPago> EstadoPago => Set<EstadoPago>();
    public DbSet<ResultadoEntrega> ResultadoEntrega => Set<ResultadoEntrega>();
    public DbSet<TipoActor> TipoActor => Set<TipoActor>();
    public DbSet<TipoNotificacion> TipoNotificacion => Set<TipoNotificacion>();
    public DbSet<CanalNotificacion> CanalNotificacion => Set<CanalNotificacion>();
    public DbSet<EstadoNotificacion> EstadoNotificacion => Set<EstadoNotificacion>();
    public DbSet<Archivo> Archivo => Set<Archivo>();
    public DbSet<UsuarioInterno> UsuarioInterno => Set<UsuarioInterno>();
    public DbSet<Categoria> Categoria => Set<Categoria>();
    public DbSet<Producto> Producto => Set<Producto>();
    public DbSet<VarianteProducto> VarianteProducto => Set<VarianteProducto>();
    public DbSet<Atributo> Atributo => Set<Atributo>();
    public DbSet<ValorAtributo> ValorAtributo => Set<ValorAtributo>();
    public DbSet<ProductoAtributo> ProductoAtributo => Set<ProductoAtributo>();
    public DbSet<VarianteAtributo> VarianteAtributo => Set<VarianteAtributo>();
    public DbSet<ProductoImagen> ProductoImagen => Set<ProductoImagen>();
    public DbSet<ZonaPersonalizacion> ZonaPersonalizacion => Set<ZonaPersonalizacion>();
    public DbSet<PlantillaVarianteZona> PlantillaVarianteZona => Set<PlantillaVarianteZona>();
    public DbSet<AreaEntrega> AreaEntrega => Set<AreaEntrega>();
    public DbSet<Personalizacion> Personalizacion => Set<Personalizacion>();
    public DbSet<PersonalizacionZona> PersonalizacionZona => Set<PersonalizacionZona>();
    public DbSet<Carrito> Carrito => Set<Carrito>();
    public DbSet<DetalleCarrito> DetalleCarrito => Set<DetalleCarrito>();
    public DbSet<Orden> Orden => Set<Orden>();
    public DbSet<DetalleOrden> DetalleOrden => Set<DetalleOrden>();
    public DbSet<Pago> Pago => Set<Pago>();
    public DbSet<HistorialEstadoOrden> HistorialEstadoOrden => Set<HistorialEstadoOrden>();
    public DbSet<IntentoEntrega> IntentoEntrega => Set<IntentoEntrega>();
    public DbSet<Notificacion> Notificacion => Set<Notificacion>();
    public DbSet<BitacoraAuditoria> BitacoraAuditoria => Set<BitacoraAuditoria>();
    public DbSet<BuyerBiometricCredentialEntity> BiometriaComprador => Set<BuyerBiometricCredentialEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NextTechDbContext).Assembly);
    }
}
