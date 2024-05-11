# Ecommerce API

Welcome to the Ecommerce API repository! This API provides various functionalities for managing products, categories, user authentication, reviews, and orders for an ecommerce platform. It is a project in progress!

## Features

- **Product Management:**
  - Add, edit, delete, and retrieve products
- **Category Management:**
  - Add, edit, delete, and retrieve product categories
- **User Authentication:**
  - Register and login with username and password
  - JWT token authentication and authorization
- **Review System:**
  - Add reviews to products
  - Get reviews of a product
- **Order Placement:**
  - Place orders


## Technologies Used

- ASP.NET Core 8 Web API, PostgreSQL, Entity Framework Core 8, Dapper, FluentValidation, MediatR, Docker
- Architecture Patterns: Clean architecture, Domain-driven design (DDD), CQRS by implementing Mediator pattern using MediatR package

## Getting Started

To get started with the Ecommerce API, follow these steps:

1. Clone this repository to your local machine.
2. Install Docker Desktop for windows.
3. Navigate to the project directory in your terminal.
4. Run the command `docker-compose up`.

## API Endpoints


### Users

- **POST /api/users/register**: Registers a new user.
  - Request Body:
    ```json
    {
      "firstName": "John",
      "lastName": "Doe",
      "email": "john.doe@example.com",
      "password": "Password123!"
    }
    ```
  - Response: Success (200)

- **POST /api/users/login**: Logs in a user.
  - Request Body:
    ```json
    {
      "email": "john.doe@example.com",
      "password": "Password123!"
    }
    ```
  - Response: Success (200)

### Products

- **GET /api/products**: Retrieves a list of products. Optionally filter by product name.
  - Query Parameters:
    - `name` (string) - Filter products by name.
  - Response: Success (200)

- **POST /api/products**: Creates a new product.
  - Request Body:
    ```json
    {
      "name": "Laptop",
      "description": "High-performance laptop with the latest specifications.",
      "priceCurrency": "USD",
      "priceAmount": 1099.99,
      "quantity": 50,
      "productCategoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    }
    ```
  - Response: Success (200)

- **GET /api/products/{id}**: Retrieves details of a specific product.
  - Request Parameters:
    - `id` (string, format: uuid) - ID of the product to retrieve.
  - Response: Success (200)

- **PUT /api/products/{id}**: Updates details of a specific product.
  - Request Parameters:
    - `id` (string, format: uuid) - ID of the product to update.
  - Request Body:
    ```json
    {
      "name": "Updated Laptop",
      "description": "Updated description of the laptop.",
      "priceCurrency": "USD",
      "priceAmount": 1199.99,
      "quantity": 60,
      "productCategoryId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
    }
    ```
  - Response: Success (200)

- **DELETE /api/products/{id}**: Deletes a specific product.
  - Request Parameters:
    - `id` (string, format: uuid) - ID of the product to delete.
  - Response: Success (200)

### Product Categories

- **GET /api/productcategories**: Retrieves a list of product categories.
  - *No request body.*
  - Response: Success (200)

- **POST /api/productcategories**: Creates a new product category.
  - Request Body:
    ```json
    {
      "name": "Electronics",
      "code": "electronics"
    }
    ```
  - Response: Success (200)

- **GET /api/productcategories/{id}**: Retrieves details of a specific product category.
  - Request Parameters:
    - `id` (string, format: uuid) - ID of the product category to retrieve.
  - Response: Success (200)

- **PUT /api/productcategories/{id}**: Updates details of a specific product category.
  - Request Parameters:
    - `id` (string, format: uuid) - ID of the product category to update.
  - Request Body:
    ```json
    {
      "name": "Updated Electronics",
      "code": "electronics"
    }
    ```
  - Response: Success (200)

- **DELETE /api/productcategories/{id}**: Deletes a specific product category.
  - Request Parameters:
    - `id` (string, format: uuid) - ID of the product category to delete.
  - Response: Success (200)

### Reviews

- **POST /api/reviews**: Posts a review for a product.
  - Request Body:
    ```json
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "rating": 5,
      "comment": "This laptop exceeded my expectations. Highly recommended!"
    }
    ```
  - Response: Success (200)

### Orders

- **GET /api/orders**: Retrieves a list of orders.
  - *No request body.*
  - Response: Success (200)

- **POST /api/orders**: Places a new order.
  - Request Body:
    ```json
    {
      "orderItems": [
        {
          "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "quantity": 1
        },
        {
          "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
          "quantity": 2
        }
      ]
    }
    ```
  - Response: Success (200)
