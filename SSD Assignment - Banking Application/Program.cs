using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;

namespace Banking_Application
{
    public class Program
    {

        // Validates that a name is not empty and contains only valid characters
        private static bool IsValidName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;
            
            // At least put one letter...
            return name.Any(char.IsLetter);
        }

        // No Zeroes!
        private static bool IsValidAmount(double amount)
        {
            return amount > 0 && !double.IsNaN(amount) && !double.IsInfinity(amount);
        }

        // Account Number is in the correct format, right?
        private static bool IsValidAccountNumber(string accountNo)
        {
            if (string.IsNullOrWhiteSpace(accountNo))
                return false;
            
            // GUID format pls
            return Guid.TryParse(accountNo, out _);
        }

        // Reads and validates a numeric input
        private static bool TryReadDouble(string prompt, out double value, bool allowZero = false)
        {
            Console.WriteLine(prompt);
            string input = Console.ReadLine();
            
            if (double.TryParse(input, out value))
            {
                if (allowZero)
                    return value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value);
                else
                    return IsValidAmount(value);
            }
            
            return false;
        }

        // Safely reads a non-empty string from console
        private static string ReadNonEmptyString(string prompt)
        {
            string input;
            do
            {
                Console.WriteLine(prompt);
                input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                    Console.WriteLine("Input cannot be empty. Please try again.");
                    
            } while (string.IsNullOrWhiteSpace(input));
            
            return input.Trim();
        }

        public static void Main(string[] args)
        {
            string domain = "ITSLIGO.LAN";
            UserPrincipal currentUser = null;
            bool isAuthenticated = false;

            using (var adAuth = new ActiveDirectoryAuthenticator(domain))
            {
                for (int attempts = 0; attempts < 3 && !isAuthenticated; attempts++)
                {
                    Console.Write("Enter your AD username: ");
                    string username = Console.ReadLine();
                    Console.Write("Enter your AD password: ");
                    string password = ReadPassword();

                    if (adAuth.Authenticate(username, password, out currentUser))
                    {
                        if (adAuth.IsInRole(currentUser, "Bank Teller"))
                        {
                            isAuthenticated = true;
                            EventLogger.LogAuth(username, true, "Bank Teller");
                            Console.WriteLine("Login successful!");
                        }
                        else
                        {
                            EventLogger.LogAuth(username, false, "Not in Bank Teller group");
                            Console.WriteLine("You are not in the Bank Teller group.");
                        }
                    }
                    else
                    {
                        EventLogger.LogAuth(username, false, "Invalid credentials");
                        Console.WriteLine("Login failed.");
                    }
                }
            }

            if (!isAuthenticated)
            {
                Console.WriteLine("Authentication failed. Exiting.");
                return;
            }

            // Read le password
            static string ReadPassword()
            {
                string pass = "";
                ConsoleKeyInfo key;
                do
                {
                    key = Console.ReadKey(true);
                    if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                    {
                        pass += key.KeyChar;
                        Console.Write("*");
                    }
                    else if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, pass.Length - 1);
                        Console.Write("\b \b");
                    }
                } while (key.Key != ConsoleKey.Enter);
                Console.WriteLine();
                return pass;
            }


            Data_Access_Layer dal = Data_Access_Layer.getInstance();
            dal.loadBankAccounts();

            string tellerName = currentUser?.SamAccountName ?? "Unknown Teller";

            bool running = true;

            do
            {

                Console.WriteLine("");
                Console.WriteLine("***Banking Application Menu***");
                Console.WriteLine("1. Add Bank Account");
                Console.WriteLine("2. Close Bank Account");
                Console.WriteLine("3. View Account Information");
                Console.WriteLine("4. Make Lodgement");
                Console.WriteLine("5. Make Withdrawal");
                Console.WriteLine("6. Exit");
                Console.WriteLine("CHOOSE OPTION:");
                String option = Console.ReadLine();
                
                switch(option)
                {
                    case "1":
                        String accountType = "";
                        int loopCount = 0;
                        
                        do
                        {

                           if(loopCount > 0)
                                Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");

                            Console.WriteLine("");
                            Console.WriteLine("***Account Types***:");
                            Console.WriteLine("1. Current Account.");
                            Console.WriteLine("2. Savings Account.");
                            Console.WriteLine("CHOOSE OPTION:");
                            accountType = Console.ReadLine();

                            loopCount++;

                        } while (!(accountType.Equals("1") || accountType.Equals("2")));

                        String name = "";
                        loopCount = 0;

                        do
                        {
                            if (loopCount > 0)
                                Console.WriteLine("INVALID NAME ENTERED - MUST CONTAIN AT LEAST ONE LETTER");

                            Console.WriteLine("Enter Name: ");
                            name = Console.ReadLine()?.Trim();

                            loopCount++;

                        } while (!IsValidName(name));

                        String addressLine1 = "";
                        loopCount = 0;

                        do
                        {
                            if (loopCount > 0)
                                Console.WriteLine("INVALID ADDRESS LINE 1 ENTERED - CANNOT BE EMPTY");

                            Console.WriteLine("Enter Address Line 1: ");
                            addressLine1 = Console.ReadLine()?.Trim();

                            loopCount++;

                        } while (string.IsNullOrWhiteSpace(addressLine1));

                        Console.WriteLine("Enter Address Line 2 (optional): ");
                        String addressLine2 = Console.ReadLine()?.Trim() ?? "";
                        
                        Console.WriteLine("Enter Address Line 3 (optional): ");
                        String addressLine3 = Console.ReadLine()?.Trim() ?? "";

                        String town = "";
                        loopCount = 0;

                        do
                        {
                            if (loopCount > 0)
                                Console.WriteLine("INVALID TOWN ENTERED - CANNOT BE EMPTY");

                            Console.WriteLine("Enter Town: ");
                            town = Console.ReadLine()?.Trim();

                            loopCount++;

                        } while (string.IsNullOrWhiteSpace(town));

                        double balance = -1;
                        loopCount = 0;

                        do
                        {
                            if (loopCount > 0)
                                Console.WriteLine("INVALID OPENING BALANCE - MUST BE A POSITIVE NUMBER");

                            if (!TryReadDouble("Enter Opening Balance: ", out balance, allowZero: true))
                            {
                                Console.WriteLine("Invalid number format. Please try again.");
                            }

                            loopCount++;

                        } while (balance < 0);

                        Bank_Account ba;

                        if (Convert.ToInt32(accountType) == Account_Type.Current_Account)
                        {
                            double overdraftAmount = -1;
                            loopCount = 0;

                            do
                            {
                                if (loopCount > 0)
                                    Console.WriteLine("INVALID OVERDRAFT AMOUNT - MUST BE A POSITIVE NUMBER");

                                if (!TryReadDouble("Enter Overdraft Amount: ", out overdraftAmount, allowZero: true))
                                {
                                    Console.WriteLine("Invalid number format. Please try again.");
                                }

                                loopCount++;

                            } while (overdraftAmount < 0);

                            ba = new Current_Account(name, addressLine1, addressLine2, addressLine3, town, balance, overdraftAmount);
                        }

                        else
                        {

                            double interestRate = -1;
                            loopCount = 0;

                            do
                            {
                                if (loopCount > 0)
                                    Console.WriteLine("INVALID INTEREST RATE - MUST BE A POSITIVE NUMBER");

                                if (!TryReadDouble("Enter Interest Rate: ", out interestRate, allowZero: true))
                                {
                                    Console.WriteLine("Invalid number format. Please try again.");
                                }

                                loopCount++;

                            } while (interestRate < 0);

                            ba = new Savings_Account(name, addressLine1, addressLine2, addressLine3, town, balance, interestRate);
                        }

                        String accNo = dal.addBankAccount(ba);

                        Console.WriteLine("New Account Number Is: " + accNo);

                        // Log the account creation
                        EventLogger.LogTransaction(tellerName, accNo, ba.name, "Account Creation", balance, "SUCCESS", "");

                        break;
                    case "2":
                        Console.WriteLine("Enter Account Number: ");
                        accNo = Console.ReadLine()?.Trim();

                        if (!IsValidAccountNumber(accNo))
                        {
                            Console.WriteLine("Invalid account number format.");
                            EventLogger.LogTransaction(tellerName, accNo ?? "Invalid", "Unknown", "Account Closure", 0, "FAIL", "Invalid account number format");
                            break;
                        }

                        ba = dal.findBankAccountByAccNo(accNo);

                        if (ba is null)
                        {
                            Console.WriteLine("Account Does Not Exist");
                            EventLogger.LogTransaction(tellerName, accNo, "Unknown", "Account Closure", 0, "FAIL", "Account not found");
                        }
                        else
                        {
                            Console.WriteLine(ba.ToString());

                            String ans = "";

                            do
                            {

                                Console.WriteLine("Proceed With Deletion (Y/N)?"); 
                                ans = Console.ReadLine();

                                switch (ans)
                                {
                                    // Deletion logic
                                    case "Y":
                                    case "y":
                                        // Admin approval required
                                        Console.WriteLine("Admin approval required to delete account.");
                                        using (var adAuth = new ActiveDirectoryAuthenticator(domain))
                                        {
                                            Console.Write("Admin username: ");
                                            string adminUser = Console.ReadLine();
                                            Console.Write("Admin password: ");
                                            string adminPass = ReadPassword();

                                            if (adAuth.Authenticate(adminUser, adminPass, out var adminPrincipal) &&
                                                adAuth.IsInRole(adminPrincipal, "Bank Teller Administrator"))
                                            {
                                                EventLogger.LogAuth(adminUser, true, "Bank Teller Administrator");
                                                dal.closeBankAccount(accNo);
                                                EventLogger.LogTransaction(tellerName, accNo, ba.name, "Account Closure", ba.balance, "SUCCESS", "");
                                            }
                                            else
                                            {
                                                EventLogger.LogAuth(adminUser, false, "Admin approval failed");
                                                Console.WriteLine("Admin approval failed. Account not deleted.");
                                                EventLogger.LogTransaction(tellerName, accNo, ba.name, "Account Closure", ba.balance, "FAIL", "Admin approval failed");
                                            }
                                        }
                                        break;
                                    case "N":
                                    case "n":
                                        EventLogger.LogTransaction(tellerName, accNo, ba.name, "Account Closure", ba.balance, "FAIL", "User cancelled");
                                        break;
                                    default:
                                        Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                                        break;
                                }
                            } while (!(ans.Equals("Y") || ans.Equals("y") || ans.Equals("N") || ans.Equals("n")));
                        }

                        break;
                    case "3":
                        Console.WriteLine("Enter Account Number: ");
                        accNo = Console.ReadLine()?.Trim();

                        if (!IsValidAccountNumber(accNo))
                        {
                            Console.WriteLine("Invalid account number format.");
                            EventLogger.LogTransaction(tellerName, accNo ?? "Invalid", "Unknown", "Balance Query", 0, "FAIL", "Invalid account number format");
                            break;
                        }

                        ba = dal.findBankAccountByAccNo(accNo);

                        if(ba is null) 
                        {
                            Console.WriteLine("Account Does Not Exist");
                            EventLogger.LogTransaction(tellerName, accNo, "Unknown", "Balance Query", 0, "FAIL", "Account not found");
                        }
                        else
                        {
                            Console.WriteLine(ba.ToString());
                            EventLogger.LogTransaction(tellerName, accNo, ba.name, "Balance Query", 0, "SUCCESS", "");
                        }

                        break;
                    case "4": //Lodge
                        Console.WriteLine("Enter Account Number: ");
                        accNo = Console.ReadLine()?.Trim();

                        if (!IsValidAccountNumber(accNo))
                        {
                            Console.WriteLine("Invalid account number format.");
                            EventLogger.LogTransaction(tellerName, accNo ?? "Invalid", "Unknown", "Lodgement", 0, "FAIL", "Invalid account number format");
                            break;
                        }

                        ba = dal.findBankAccountByAccNo(accNo);

                        if (ba is null)
                        {
                            Console.WriteLine("Account Does Not Exist");
                            EventLogger.LogTransaction(tellerName, accNo, "Unknown", "Lodgement", 0, "FAIL", "Account not found");
                        }
                        else
                        {
                            double amountToLodge = -1;
                            loopCount = 0;

                            do
                            {
                                if (loopCount > 0)
                                    Console.WriteLine("INVALID AMOUNT - MUST BE A POSITIVE NUMBER");

                                if (!TryReadDouble("Enter Amount To Lodge: ", out amountToLodge, allowZero: false))
                                {
                                    Console.WriteLine("Invalid number format. Please try again.");
                                }

                                loopCount++;

                            } while (amountToLodge <= 0);

                            String reason = "";
                            if (amountToLodge >= 10000)
                            {
                                Console.WriteLine("Enter reason for >€10k lodgement (optional but recommended): ");
                                reason = Console.ReadLine();
                            }

                            dal.lodge(accNo, amountToLodge);
                            EventLogger.LogTransaction(tellerName, accNo, ba.name, "Lodgement", amountToLodge, "SUCCESS", reason);
                        }
                        break;
                    case "5": //Withdraw
                        Console.WriteLine("Enter Account Number: ");
                        accNo = Console.ReadLine()?.Trim();

                        if (!IsValidAccountNumber(accNo))
                        {
                            Console.WriteLine("Invalid account number format.");
                            EventLogger.LogTransaction(tellerName, accNo ?? "Invalid", "Unknown", "Withdrawal", 0, "FAIL", "Invalid account number format");
                            break;
                        }

                        ba = dal.findBankAccountByAccNo(accNo);

                        if (ba is null)
                        {
                            Console.WriteLine("Account Does Not Exist");
                            EventLogger.LogTransaction(tellerName, accNo, "Unknown", "Withdrawal", 0, "FAIL", "Account not found");
                        }
                        else
                        {
                            double amountToWithdraw = -1;
                            loopCount = 0;

                            do
                            {
                                if (loopCount > 0)
                                    Console.WriteLine("INVALID AMOUNT - MUST BE A POSITIVE NUMBER");

                                if (!TryReadDouble("Enter Amount To Withdraw (€" + ba.getAvailableFunds() + " Available): ", out amountToWithdraw, allowZero: false))
                                {
                                    Console.WriteLine("Invalid number format. Please try again.");
                                }

                                loopCount++;

                            } while (amountToWithdraw <= 0);

                            String reason = "";
                            if (amountToWithdraw >= 10000)
                            {
                                Console.WriteLine("Enter reason for >€10k withdrawal (optional but recommended): ");
                                reason = Console.ReadLine();
                            }

                            bool withdrawalOK = dal.withdraw(accNo, amountToWithdraw);

                            if(withdrawalOK == false)
                            {

                                Console.WriteLine("Insufficient Funds Available.");
                                EventLogger.LogTransaction(tellerName, accNo, ba.name, "Withdrawal", amountToWithdraw, "FAIL", "Insufficient funds");
                            }
                            else
                            {
                                EventLogger.LogTransaction(tellerName, accNo, ba.name, "Withdrawal", amountToWithdraw, "SUCCESS", reason);
                            }
                        }
                        break;
                    case "6":
                        running = false;
                        break;
                    default:    
                        Console.WriteLine("INVALID OPTION CHOSEN - PLEASE TRY AGAIN");
                        break;
                }
                
                
            } while (running != false);

        }

    }
}