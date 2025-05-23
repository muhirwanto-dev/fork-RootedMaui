﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RootedBack.Data;
using SharedLibraryy.Models;
namespace RootedBack.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FarmersController : ControllerBase
    {
        private readonly RootedDBContext _context;


        public FarmersController(RootedDBContext context)
        {
            _context = context;
        }

        // GET: api/Farmers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Farmer>>> GetFarmers()
        {
            return await _context.Farmers
    .Include(f => f.Specification)
    .ToListAsync();

        }

        // GET: api/Farmers/5
        // GET: api/Farmers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Farmer>> GetFarmer(int id)
        {
            try
            {
                var farmer = await _context.Farmers
                    .Include(f => f.Specification)
                    .FirstOrDefaultAsync(f => f.FarmerId == id);

                if (farmer == null)
                {
                    return NotFound($"Farmer with ID {id} not found.");
                }

                return Ok(farmer);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }




        // PUT: api/Farmers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFarmer(int id, Farmer farmer)
        {
            if (id != farmer.FarmerId)
            {
                return BadRequest();
            }

            _context.Entry(farmer).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FarmerExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Farmers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Farmer>> PostFarmer(Farmer farmer)
        {
            _context.Farmers.Add(farmer);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFarmer", new { id = farmer.FarmerId }, farmer);
        }

        [HttpGet("with-reviews/{farmerId}")]
        public async Task<ActionResult<Farmer>> GetFarmerWithReviews(int farmerId)
        {
            var farmer = await _context.Farmers
                .Include(f => f.Specification)  // Include the farmer's specification
                .Include(f => f.Reviews)        // Include the farmer's reviews
                .FirstOrDefaultAsync(f => f.FarmerId == farmerId);

            if (farmer == null)
            {
                return NotFound();
            }

            return Ok(farmer);
        }

        // DELETE: api/Farmers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFarmer(int id)
        {
            var farmer = await _context.Farmers.FindAsync(id);
            if (farmer == null)
            {
                return NotFound();
            }

            _context.Farmers.Remove(farmer);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        private bool FarmerExists(int id)
        {
            return _context.Farmers.Any(e => e.FarmerId == id);
        }
        [HttpPost("upload-certificate")]
        public async Task<IActionResult> UploadCertificate(IFormFile file, int farmerId)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty.");

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/certificates");
            Directory.CreateDirectory(uploadsFolder);

            string fileName = $"farmer_{farmerId}.pdf";
            string filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string relativePath = $"/certificates/{fileName}";

            using (var connection = new SqlConnection("DefaultConnection"))
            {
                string query = "UPDATE Farmer SET CertificatePath = @FilePath WHERE FarmerID = @FarmerID";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@FilePath", relativePath);
                    command.Parameters.AddWithValue("@FarmerID", farmerId);
                    connection.Open();
                    command.ExecuteNonQuery();
                }
            }

            return Ok(new { Message = "File uploaded successfully!", Path = relativePath });
        }


        [HttpPost("Login")]
        public async Task<ActionResult<Farmer>> SignInFarmer(FarmerLoginRequest request)
        {
            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(f => f.Email == request.Email && f.Password == request.Password);

            if (farmer == null)
            {
                return BadRequest("البريد الإلكتروني أو كلمة المرور غير صحيحة.");
            }

            if (farmer.VerificationStatus != true)
            {
                return Unauthorized("ليس لديك صلاحية للدخول. يرجى الانتظار حتى تتم الموافقة عليك من قبل الإدارة.");
            }

            return Ok(farmer);
        }


        public class FarmerLoginRequest
        {
            public string Email { get; set; }
            public string Password { get; set; }

        }
        //[HttpPost("ResetPassword")]
        //public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.NewPassword))
        //        return BadRequest("البريد الإلكتروني أو كلمة المرور غير صحيحة");

        //    var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.Email == request.Email);
        //    if (farmer == null)
        //        return NotFound("لم يتم العثور على مستخدم بهذا البريد الإلكتروني");

        //    // يفضل تشفير كلمة المرور قبل تخزينها
        //    farmer.Password = (request.NewPassword); // أو فقط: request.NewPassword إذا غير مشفر

        //    await _context.SaveChangesAsync();

        //    return Ok("تم تحديث كلمة المرور بنجاح");
        //}



        public class ResetPasswordRequest
        {
            public string Email { get; set; }
            public string NewPassword { get; set; }
        }

        [HttpPut("UpdateProfile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber) || string.IsNullOrWhiteSpace(request.ImageUrl))
                return BadRequest("رقم الجوال أو رابط الصورة غير صحيح");

            // استخدام البريد من التوكن
            var email = User.Identity?.Name;
            if (string.IsNullOrEmpty(email))
                return Unauthorized("لم يتم تحديد هوية المستخدم");

            var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.Email == email);
            if (farmer == null)
                return NotFound("لم يتم العثور على المزارع");

            // التحديث
            farmer.PhoneNumber = request.PhoneNumber;
            farmer.ImageUrl = request.ImageUrl;

            await _context.SaveChangesAsync();

            return Ok("تم تحديث البيانات بنجاح");
        }


    }
}
    public class UpdateProfileRequest
    {
        public string PhoneNumber { get; set; }
        public string ImageUrl { get; set; }
    }







