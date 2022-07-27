using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

# nullable disable
namespace Api.Database.Models
{
    public class Orientation
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float W { get; set; }
        public virtual Frame Frame {get; set; }
        public Orientation()
        {
            X = 0;
            Y = 0;
            Z = 0;
            W = 1;
            Frame = new Frame();
        }
        public Orientation(float x = 0, float y = 0, float z = 0, float w = 1)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
            Frame = new Frame();
        }
    }

    public class Position
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public virtual Frame Frame {get; set; }
        public Position()
        {
            X = 0;
            Y = 0;
            Z = 0;
            Frame = new Frame();
        }
        public Position(float x = 0, float y = 0, float z = 0)
        {
            X = x;
            Y = y;
            Z = z;
            Frame = new Frame();
        }
    }

    public class Pose
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public virtual Position Position { get; set; }
        public virtual Orientation Orientation { get; set; }
        public virtual Frame Frame {get; set; }
        public Pose ()
        {
            Position = new Position();
            Orientation = new Orientation();
            Frame = new Frame();
        }
        public Pose (float x_pos, float y_pos, float z_pos, float x_ori, float y_ori, float z_ori, float w)
        {
            Position = new Position(x_pos, y_pos, z_pos);
            Orientation = new Orientation(x_ori, y_ori, z_ori, w);
            Frame = new Frame();
        }
    }

    public class Frame 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; }
        public string Name { get; set; }
        public Frame ()
        {
            Name = "defaultFrame";
        }
    }
}