using HotelManagement.Constants;
using HotelManagement.Models;

namespace HotelManagement.Data
{
    public static class SeedData
    {
        public static void Initialize(HotelDbContext context)
        {
            SeedUsers(context);
            SeedRoomTypes(context);
            SeedRooms(context);
            SeedServices(context);
        }

        private static void SeedUsers(HotelDbContext context)
        {
            if (context.Users.Any())
            {
                return;
            }

            var users = new List<User>
            {
                new User
                {
                    FullName = "System Admin",
                    Email = "admin@gmail.com",
                    PhoneNumber = "0900000001",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = UserRoles.Admin,
                    Status = UserStatuses.Active,
                    CreatedAt = DateTime.Now
                },
                new User
                {
                    FullName = "Receptionist Demo",
                    Email = "receptionist@gmail.com",
                    PhoneNumber = "0900000002",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = UserRoles.Receptionist,
                    Status = UserStatuses.Active,
                    CreatedAt = DateTime.Now
                },
                new User
                {
                    FullName = "Customer Demo",
                    Email = "customer@gmail.com",
                    PhoneNumber = "0900000003",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    Role = UserRoles.Customer,
                    IdentityNumber = "012345678901",
                    Address = "Ha Noi",
                    Status = UserStatuses.Active,
                    CreatedAt = DateTime.Now
                }
            };

            context.Users.AddRange(users);
            context.SaveChanges();
        }

        private static void SeedRoomTypes(HotelDbContext context)
        {
            if (context.RoomTypes.Any())
            {
                return;
            }

            var roomTypes = new List<RoomType>
            {
                new RoomType
                {
                    Name = "Standard",
                    Description = "Phòng tiêu chuẩn, phù hợp cho 1-2 khách.",
                    Price = 500000,
                    Capacity = 2,
                    BedType = "1 giường đôi",
                    ThumbnailUrl = "/images/rooms/standard.jpg",
                    Status = "Active",
                    CreatedAt = DateTime.Now
                },
                new RoomType
                {
                    Name = "Deluxe",
                    Description = "Phòng cao cấp hơn Standard, không gian rộng rãi.",
                    Price = 800000,
                    Capacity = 2,
                    BedType = "1 giường đôi lớn",
                    ThumbnailUrl = "/images/rooms/deluxe.jpg",
                    Status = "Active",
                    CreatedAt = DateTime.Now
                },
                new RoomType
                {
                    Name = "Suite",
                    Description = "Phòng hạng sang, có khu vực tiếp khách riêng.",
                    Price = 1500000,
                    Capacity = 4,
                    BedType = "2 giường đôi",
                    ThumbnailUrl = "/images/rooms/suite.jpg",
                    Status = "Active",
                    CreatedAt = DateTime.Now
                },
                new RoomType
                {
                    Name = "Family",
                    Description = "Phòng gia đình, phù hợp cho nhóm khách.",
                    Price = 1200000,
                    Capacity = 4,
                    BedType = "2 giường đôi",
                    ThumbnailUrl = "/images/rooms/family.jpg",
                    Status = "Active",
                    CreatedAt = DateTime.Now
                }
            };

            context.RoomTypes.AddRange(roomTypes);
            context.SaveChanges();
        }

        private static void SeedRooms(HotelDbContext context)
        {
            if (context.Rooms.Any())
            {
                return;
            }

            var standard = context.RoomTypes.First(rt => rt.Name == "Standard");
            var deluxe = context.RoomTypes.First(rt => rt.Name == "Deluxe");
            var suite = context.RoomTypes.First(rt => rt.Name == "Suite");
            var family = context.RoomTypes.First(rt => rt.Name == "Family");

            var rooms = new List<Room>
            {
                new Room
                {
                    RoomNumber = "101",
                    RoomTypeId = standard.Id,
                    Floor = 1,
                    Status = RoomStatuses.Available,
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    RoomNumber = "102",
                    RoomTypeId = standard.Id,
                    Floor = 1,
                    Status = RoomStatuses.Available,
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    RoomNumber = "201",
                    RoomTypeId = deluxe.Id,
                    Floor = 2,
                    Status = RoomStatuses.Available,
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    RoomNumber = "202",
                    RoomTypeId = deluxe.Id,
                    Floor = 2,
                    Status = RoomStatuses.Available,
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    RoomNumber = "301",
                    RoomTypeId = suite.Id,
                    Floor = 3,
                    Status = RoomStatuses.Available,
                    CreatedAt = DateTime.Now
                },
                new Room
                {
                    RoomNumber = "401",
                    RoomTypeId = family.Id,
                    Floor = 4,
                    Status = RoomStatuses.Available,
                    CreatedAt = DateTime.Now
                }
            };

            context.Rooms.AddRange(rooms);
            context.SaveChanges();
        }

        private static void SeedServices(HotelDbContext context)
        {
            if (context.Services.Any())
            {
                return;
            }

            var services = new List<Service>
            {
                new Service
                {
                    Name = "Ăn sáng",
                    Category = "Food",
                    Unit = "Suất",
                    Price = 100000,
                    Status = "Active",
                    CreatedAt = DateTime.Now
                },
                new Service
                {
                    Name = "Giặt ủi",
                    Category = "Laundry",
                    Unit = "Kg",
                    Price = 50000,
                    Status = "Active",
                    CreatedAt = DateTime.Now
                },
                new Service
                {
                    Name = "Mini Bar",
                    Category = "Room",
                    Unit = "Lần",
                    Price = 150000,
                    Status = "Active",
                    CreatedAt = DateTime.Now
                },
                new Service
                {
                    Name = "Thuê xe",
                    Category = "Transport",
                    Unit = "Ngày",
                    Price = 300000,
                    Status = "Active",
                    CreatedAt = DateTime.Now
                },
                new Service
                {
                    Name = "Spa",
                    Category = "Relax",
                    Unit = "Lần",
                    Price = 400000,
                    Status = "Active",
                    CreatedAt = DateTime.Now
                }
            };

            context.Services.AddRange(services);
            context.SaveChanges();
        }
    }
}
