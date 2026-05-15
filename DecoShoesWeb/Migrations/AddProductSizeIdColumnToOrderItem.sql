-- Add ProductSizeID column to OrderItems table if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'OrderItems' AND COLUMN_NAME = 'ProductSizeID')
BEGIN
    ALTER TABLE [OrderItems]
    ADD [ProductSizeID] int NULL;

    -- Add foreign key constraint
    ALTER TABLE [OrderItems]
    ADD CONSTRAINT [FK_OrderItems_ProductSizes_ProductSizeID]
    FOREIGN KEY ([ProductSizeID])
    REFERENCES [ProductSizes] ([ProductSizeID]);

    -- Create index for the foreign key
    CREATE INDEX [IX_OrderItems_ProductSizeID]
    ON [OrderItems] ([ProductSizeID]);
END
