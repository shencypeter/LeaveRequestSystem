ALTER TABLE 
  supplier_1st_assess 
ADD 
  risk_level varchar(10);
EXEC sys.sp_addextendedproperty @name = N'MS_Description', 
@value = N '­·ÀI­È', 
@level0type = N 'SCHEMA', 
@level0name = N 'dbo', 
@level1type = N 'TABLE', 
@level1name = N 'supplier_1st_assess', 
@level2type = N 'COLUMN', 
@level2name = N 'risk_level' GO
