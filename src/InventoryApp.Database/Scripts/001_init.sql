PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS products (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL,
    size TEXT NULL,
    category TEXT NULL,
    unit TEXT NOT NULL,
    barcode TEXT NULL,
    color TEXT NULL,
    note TEXT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS opening_stock (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    product_id INTEGER NOT NULL,
    qty REAL NOT NULL,
    created_at TEXT NOT NULL,
    note TEXT NULL,
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS reporting_periods (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    start_date TEXT NOT NULL,
    end_date TEXT NOT NULL,
    is_closed INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS stock_documents (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    doc_no TEXT NOT NULL UNIQUE,
    doc_type TEXT NOT NULL,
    reference_doc_no TEXT NULL,
    reversed_document_id INTEGER NULL,
    customer_name TEXT NULL,
    sale_employee_name TEXT NULL,
    request_employee_name TEXT NULL,
    area TEXT NULL,
    address TEXT NULL,
    phone TEXT NULL,
    payment_method TEXT NULL,
    document_status TEXT NOT NULL,
    note TEXT NULL,
    reporting_period_id INTEGER NULL,
    doc_date TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    FOREIGN KEY (reporting_period_id) REFERENCES reporting_periods(id),
    FOREIGN KEY (reversed_document_id) REFERENCES stock_documents(id)
);

CREATE TABLE IF NOT EXISTS stock_document_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    document_id INTEGER NOT NULL,
    product_id INTEGER NOT NULL,
    qty REAL NOT NULL,
    stock_effect_type TEXT NOT NULL,
    item_status TEXT NULL,
    note TEXT NULL,
    FOREIGN KEY (document_id) REFERENCES stock_documents(id),
    FOREIGN KEY (product_id) REFERENCES products(id)
);

CREATE TABLE IF NOT EXISTS roles (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    code TEXT NOT NULL UNIQUE,
    name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    username TEXT NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    display_name TEXT NOT NULL,
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS user_roles (
    user_id INTEGER NOT NULL,
    role_id INTEGER NOT NULL,
    PRIMARY KEY (user_id, role_id),
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (role_id) REFERENCES roles(id)
);

CREATE INDEX IF NOT EXISTS idx_products_code ON products(code);
CREATE INDEX IF NOT EXISTS idx_documents_doc_no ON stock_documents(doc_no);
CREATE INDEX IF NOT EXISTS idx_documents_doc_date ON stock_documents(doc_date);
CREATE INDEX IF NOT EXISTS idx_documents_doc_type ON stock_documents(doc_type);
CREATE INDEX IF NOT EXISTS idx_items_document_id ON stock_document_items(document_id);
CREATE INDEX IF NOT EXISTS idx_items_product_id ON stock_document_items(product_id);
CREATE INDEX IF NOT EXISTS idx_items_stock_effect_type ON stock_document_items(stock_effect_type);
CREATE INDEX IF NOT EXISTS idx_items_status ON stock_document_items(item_status);
