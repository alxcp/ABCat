//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ABCat.DB.Entity.UserData
{
    #pragma warning disable 1573
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Common;
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration;
    using System.Data.Entity.Infrastructure;
    
    internal partial class RecordUserData_Mapping : EntityTypeConfiguration<RecordUserData>
    {
        public RecordUserData_Mapping()
        {                        
              this.HasKey(t => t.ID);        
              this.ToTable("RecordUserDataSet");
              this.Property(t => t.ID).HasColumnName("ID");
              this.Property(t => t.RecordKey).HasColumnName("RecordKey").IsRequired();
              this.Property(t => t.RecordGroupKey).HasColumnName("RecordGroupKey").IsRequired();
              this.Property(t => t.LocalPath).HasColumnName("LocalPath");
         }
    }
}

