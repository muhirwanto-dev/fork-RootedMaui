﻿using SharedLibraryy.Models;
using System.Net.Http.Json;

namespace AdminApp
{
    public class AdminServic
    {
        private readonly HttpClient _httpClient;
        private static readonly string _baseUrl =
#if ANDROID
       "http://10.0.2.2:5140/api";
#else
        "https://localhost:7168/api"; 
#endif

        public AdminServic(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<List<Admin>> GetAdminsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Admin>>($"{_baseUrl}/Admins");
        }

        public async Task<Admin> GetAdminByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<Admin>($"{_baseUrl}/Admins/{id}");
        }

        public async Task<bool> CreateAdminAsync(Admin admin)
        {
            var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/Admins", admin);
            return response.IsSuccessStatusCode;
        }
    }
}
