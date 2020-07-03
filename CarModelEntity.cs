
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Dataflow.SaicEnergyTracker
{
    public class CarModelContext : DbContext
    {
        public DbSet<CarEnergyModelEntity> CarEnergyModels { get; set; }

        public CarModelContext(DbContextOptions<CarModelContext> options) : base(options)
        {

        }

    }

    [Table("car_energy_model")]
    public class CarEnergyModelEntity
    {
        public CarEnergyModelEntity() { }

        [JsonIgnore]
        [Key]
        public Guid Id { get; set; }

        [Column("car_version")]
        public string Car_Version { get; set; }
        [Column("car_id")]
        public string Car_Id { get; set; }

        [Column("user_id")]
        public string User_Id { get; set; }

        [Column("so_version")]
        public string So_Version { get; set; }

        [Column("config_version")]
        public string Config_Version { get; set; }

        [Column("config_content")]
        public string Config_Content { get; set; }

        [Column("uploadtime")]
        public DateTime? UploadTime { get; set; }
    }
}
