ALTER TABLE qualified_suppliers
ADD supplier_no varchar(50);

EXEC sys.sp_addextendedproperty @name=N'MS_Description', @value=N'¼t°Ó²Î½s' , @level0type=N'SCHEMA',@level0name=N'dbo', @level1type=N'TABLE',@level1name=N'qualified_suppliers', @level2type=N'COLUMN',@level2name=N'supplier_no'
GO
