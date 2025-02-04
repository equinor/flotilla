﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#pragma warning disable CS8618
namespace Api.Database.Models
{
    public class Installation
    {
        private string _installationCode;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        [Required]
        [MaxLength(10)]
        public string InstallationCode
        {
            get => _installationCode.ToUpper();
            set => _installationCode = value.ToUpper();
        }
    }
}
