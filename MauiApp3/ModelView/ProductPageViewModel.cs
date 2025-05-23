﻿using MauiApp3.Pages;
using MauiApp3.Services;
using SharedLibraryy.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
namespace MauiApp3.ModelView
{
    public partial class ProductPageViewModel : INotifyPropertyChanged

    {
        private readonly IProductService productService;

        private readonly INavigation _navigation;
        public ObservableCollection<Product> products { get; set; } = new();
        public ObservableCollection<Category> Categories { get; set; } = new();
        public ObservableCollection<Product> FilteredProducts { get; set; } = new ObservableCollection<Product>();

        public Command<int> ChangeCategoryCommand { get; }
        private readonly ICartService _cartService;

        public Command<Product> AddToCartCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;
        public ProductPageViewModel(int selectedCategoryId, IProductService productService , ICartService cartService, INavigation navigation)
        {
            this.productService = productService;
            _navigation = navigation;

            SelectedCategoryId = selectedCategoryId;
            FilterProducts();

            ChangeCategoryCommand = new Command<int>(categoryId => SetCategory(categoryId));
            ViewProductCommand = new Command<Product>(OnProductSelected);
            this._cartService = cartService;
            AddToCartCommand = new Command<Product>(AddToCart);


            GetCategories();
            GetProduct();
        }



        private void SetCategory(int categoryId)
        {
            if (_selectedCategoryId == categoryId) return;

            _selectedCategoryId = categoryId;
            OnPropertyChanged(nameof(SelectedCategoryId));

            // After setting the category, filter products based on the new category
            FilterProducts();
        }

        private async void GetCategories()
        {
            var categories = await productService.GetCategoriesAsync();
            if (categories is null || categories.Count == 0)
                return;

            foreach (var category in categories)
            {
                var newCategory = new Category
                {
                    CategoryId = category.CategoryId,
                    CategoryName = category.CategoryName
                };
                Categories.Add(newCategory);
            }
        }

        private async void GetProduct()
        {

            var productList = await productService.GetProductAsync();
            var farmersList = await productService.GetFarmersAsync(); // Fetch all farmers

            if (productList is null || productList.Count == 0)
                return;

            products.Clear();
            foreach (var product in productList)
            {
                // Find the farmer for this product
                var farmer = farmersList.FirstOrDefault(f => f.FarmerId == product.FarmerId);

                products.Add(new Product
                {
                    ProductId = product.ProductId,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    FarmerId = product.FarmerId,
                    Farmer = product.Farmer, 
                    CategoryId = product.CategoryId,
                    Quantity = product.Quantity,
                    Weight = product.Weight,
                    ImageUrl = product.ImageUrl,
                    Unit = product.Unit
                });
            }

            FilterProducts();
        }




        private int _selectedCategoryId;
        public int SelectedCategoryId
        {
            get => _selectedCategoryId;
            set
            {
                _selectedCategoryId = value;
                OnPropertyChanged(nameof(SelectedCategoryId));
            }
        }



        public ProductPageViewModel()
        {
            ChangeCategoryCommand = new Command<int>(OnCategoryChanged);
            AddToCartCommand = new Command<Product>((_) => { });
        }

        private void OnCategoryChanged(int categoryId)
        {
            SelectedCategoryId = categoryId;
            FilterProducts();
        }

        private int _selectedFarmerId;
        public int SelectedFarmerId
        {
            get => _selectedFarmerId;
            set
            {
                if (_selectedFarmerId != value)
                {
                    _selectedFarmerId = value;
                    FilterProducts();
                    OnPropertyChanged(nameof(SelectedFarmerId));
                    OnPropertyChanged(nameof(FilteredProducts));
                }
            }
        }

        private void FilterProducts()
        {
            if (products == null) return;

            var filtered = products
                .Where(p => p.CategoryId == SelectedCategoryId &&
                            (SelectedFarmerId == 0 || p.FarmerId == SelectedFarmerId))
                .ToList();

            FilteredProducts.Clear();
            foreach (var product in filtered)
            {
                FilteredProducts.Add(product);
            }

            OnPropertyChanged(nameof(FilteredProducts));
        }



        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        public Command<Product> ViewProductCommand { get; }



        private async void OnProductSelected(Product product)
        {
            if (product == null)
            {
                Console.WriteLine("Error: Selected product is null");
                return;
            }
            await _navigation.PushAsync(new Pages.Consumers.ProductInfo(product, productService));
        }
        private async void AddToCart(Product product)
        {
            if (product == null)
                return;

            // Check if the product is already in the cart
            var existingCartItem = _cartService.GetCart().FirstOrDefault(c => c.ProductId == product.ProductId);

            if (existingCartItem == null)
            {
                // Create a new Cart item with the required properties (e.g., Quantity, Amount)
                var cartItem = new Cart
                {
                    ProductId = product.ProductId,
                    Product = product,
                    Quantity = 1, // Default quantity when adding for the first time
                    Price = (decimal)product.Price,
                    Amount = (decimal)product.Price * 1 // Amount is price * quantity
                };

                _cartService.AddProductToCart(cartItem.Product, cartItem.Quantity);
                product.IsInCart = true; // Mark as added to cart
            }
            else
            {
                // If the product is already in the cart, you can just update the quantity or do any other logic you need
                existingCartItem.Quantity++;
                existingCartItem.Amount = existingCartItem.Price * existingCartItem.Quantity;
            }

            // Navigate to the CartPage after adding the product to the cart
            await _navigation.PushAsync(new Pages.Consumers.ShoppingCart(_cartService));
        }




    }

}