using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinancialManagement.Migrations
{
    /// <inheritdoc />
    public partial class CreateUserExpenseAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create expense accounts for existing users who only have asset accounts
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    user_record RECORD;
                    account_id TEXT;
                    account_num TEXT;
                    expense_account_name TEXT;
                    asset_account_name TEXT;
                    last_expense_number INT := 5000;
                BEGIN
                    -- Get last expense account number
                    SELECT COALESCE(
                        MAX(CAST(SPLIT_PART(account_number, '-', 1) AS INT)), 
                        5000
                    ) INTO last_expense_number
                    FROM accounts
                    WHERE account_number LIKE '5%';
                    
                    -- Loop through users who have asset accounts but no expense accounts
                    FOR user_record IN 
                        SELECT DISTINCT a.user_id, a.name as asset_name
                        FROM accounts a
                        WHERE a.user_id IS NOT NULL
                          AND a.type = 'Asset'
                          AND a.is_active = true
                          AND NOT EXISTS (
                              SELECT 1 FROM accounts e
                              WHERE e.user_id = a.user_id
                                AND e.type = 'Expense'
                                AND e.is_active = true
                          )
                    LOOP
                        -- Generate account details
                        account_id := gen_random_uuid()::TEXT;
                        last_expense_number := last_expense_number + 1;
                        account_num := LPAD(last_expense_number::TEXT, 4, '0') || '-EXPENSE';
                        
                        -- Derive expense account name from asset account name
                        asset_account_name := user_record.asset_name;
                        expense_account_name := REPLACE(asset_account_name, 'Personal Account', 'Expense Account');
                        
                        -- If name didn't change, create a default name
                        IF expense_account_name = asset_account_name THEN
                            expense_account_name := 'User ' || user_record.user_id || ' - Expense Account';
                        END IF;
                        
                        -- Create expense account
                        INSERT INTO accounts (
                            id, account_number, name, type, category, balance, 
                            currency, is_active, user_id, description, created_at, updated_at
                        )
                        VALUES (
                            account_id,
                            account_num,
                            expense_account_name,
                            'Expense',
                            'OperatingExpenses',
                            0.00,
                            'USD',
                            true,
                            user_record.user_id,
                            'Personal expense account for user ' || user_record.user_id,
                            NOW(),
                            NOW()
                        );
                        
                        RAISE NOTICE 'Created expense account for user %: % - %', 
                            user_record.user_id, account_num, expense_account_name;
                    END LOOP;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete user expense accounts created by this migration
            migrationBuilder.Sql(@"
                DELETE FROM accounts 
                WHERE user_id IS NOT NULL 
                  AND type = 'Expense'
                  AND category = 'OperatingExpenses'
                  AND description LIKE 'Personal expense account for user%';
            ");
        }
    }
}
