-- Users table
CREATE TABLE Users (
    UserID INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    Password NVARCHAR(255) NOT NULL, 
    FirstName NVARCHAR(50),
    LastName NVARCHAR(50),
    CreatedOn DATETIME DEFAULT GETDATE(),
    UpdatedOn DATETIME,
    IsAdmin BIT DEFAULT 0
);


-- Categories Table
CREATE TABLE Categories (
    CategoryID INT PRIMARY KEY IDENTITY(1,1),
    CategoryName NVARCHAR(100) NOT NULL,
    ParentCategoryID INT,
    FOREIGN KEY (ParentCategoryID) REFERENCES Categories(CategoryID)
);


-- Products Table
CREATE TABLE Products (
    ProductID INT PRIMARY KEY IDENTITY(1,1),
    ProductName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(MAX),
    Price DECIMAL(10, 2) NOT NULL,
    StockQuantity INT NOT NULL,
    CategoryID INT,
    ImageURL NVARCHAR(255),
    FOREIGN KEY (CategoryID) REFERENCES Categories(CategoryID)
);



-- Orders Table
CREATE TABLE Orders (
    OrderID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    OrderDate DATETIME DEFAULT GETDATE(),
    ShippingAddressID INT,
    BillingAddressID INT,
    OrderStatus NVARCHAR(50) NOT NULL,
    TotalPrice DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (UserID) REFERENCES Users(UserID),
    FOREIGN KEY (ShippingAddressID) REFERENCES Addresses(AddressID),
    FOREIGN KEY (BillingAddressID) REFERENCES Addresses(AddressID)
);

-- OrderDetails Table
CREATE TABLE OrderDetails (
    OrderDetailID INT PRIMARY KEY IDENTITY(1,1),
    OrderID INT NOT NULL,
    ProductID INT NOT NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(10,2) NOT NULL,
    FOREIGN KEY (OrderID) REFERENCES Orders(OrderID),
    FOREIGN KEY (ProductID) REFERENCES Products(ProductID)
);

-- Addresses Table
CREATE TABLE Addresses (
    AddressID INT PRIMARY KEY IDENTITY(1,1),
    UserID INT NOT NULL,
    Street NVARCHAR(255) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    State NVARCHAR(50),
    ZipCode NVARCHAR(15),
    Country NVARCHAR(100) NOT NULL,
    IsDefaultShipping BIT DEFAULT 0,
    IsDefaultBilling BIT DEFAULT 0,
    FOREIGN KEY (UserID) REFERENCES Users(UserID)
);