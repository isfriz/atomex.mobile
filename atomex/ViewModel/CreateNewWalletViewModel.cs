﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using atomex.Common;
using atomex.Models;
using atomex.ViewModel;
using Atomex;
using Atomex.Common;
using Atomex.Core;
using Atomex.Cryptography;
using Atomex.Wallet;
using NBitcoin;
using Nethereum.HdWallet;
using Serilog;

namespace atomex
{
    public class CreateNewWalletViewModel : BaseViewModel
    {

        private IAtomexApp App;

        public enum Action
        {
            Create,
            Restore
        }

        public Action CurrentAction { get; set; }

        public List<Atomex.Core.Network> Networks { get; } = new List<Atomex.Core.Network>
        {
            Atomex.Core.Network.MainNet,
            Atomex.Core.Network.TestNet
        };


        public List<CustomWordlist> Languages { get; } = new List<CustomWordlist>
        {
            new CustomWordlist { Name = "English", Wordlist = Wordlist.English },
            new CustomWordlist { Name = "French", Wordlist = Wordlist.French },
            new CustomWordlist { Name = "Japanese", Wordlist = Wordlist.Japanese },
            new CustomWordlist { Name = "Spanish", Wordlist = Wordlist.Spanish },
            new CustomWordlist { Name = "Portuguese Brazil", Wordlist = Wordlist.PortugueseBrazil },
            new CustomWordlist { Name = "Chinese Traditional", Wordlist = Wordlist.ChineseTraditional },
            new CustomWordlist { Name = "Chinese Simplified", Wordlist = Wordlist.ChineseSimplified },
        };

        public List<CustomEntropy> WordCountToEntropyLength { get; } = new List<CustomEntropy>
        {
            new CustomEntropy { WordCount = "12", Length = 128 },
            new CustomEntropy { WordCount = "15", Length = 160 },
            new CustomEntropy { WordCount = "18", Length = 192 },
            new CustomEntropy { WordCount = "21", Length = 224 },
            new CustomEntropy { WordCount = "24", Length = 256 }
        };

        private Atomex.Core.Network _network;
        public Atomex.Core.Network Network
        {
            get => _network;
            set { _network = value; OnPropertyChanged(nameof(Network)); }
        }

        private string _walletName;
        public string WalletName
        {
            get => _walletName;
            set { _walletName = value; OnPropertyChanged(nameof(WalletName)); }
        }

        public string PathToWallet { get; set; }

        private CustomWordlist _language;
        public CustomWordlist Language
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    Mnemonic = string.Empty;
                }
            }
        }

        private CustomEntropy _entropy;
        public CustomEntropy Entropy
        {
            get => _entropy;
            set
            {
                if (_entropy != value)
                {
                    _entropy = value;
                    Mnemonic = string.Empty;
                }
            }
        }

        private string _mnemonic;
        public string Mnemonic
        {
            get => _mnemonic;
            set { _mnemonic = value; OnPropertyChanged(nameof(Mnemonic)); }
        }

        private int _derivedPasswordScore;
        public int DerivedPasswordScore
        {
            get => _derivedPasswordScore;
            set { _derivedPasswordScore = value; OnPropertyChanged(nameof(DerivedPasswordScore)); }
        }

        private SecureString _derivedPassword;
        public SecureString DerivedPassword
        {
            get => _derivedPassword;
            set
            {
                _derivedPassword = value;
                DerivedPasswordScore = (int)PasswordAdvisor.CheckStrength(DerivedPassword);
                OnPropertyChanged(nameof(DerivedPassword)); }
        }

        private SecureString _derivedPasswordConfirmation;
        public SecureString DerivedPasswordConfirmation
        {
            get => _derivedPasswordConfirmation;
            set { _derivedPasswordConfirmation = value; OnPropertyChanged(nameof(DerivedPasswordConfirmation)); }
        }

        private int _storagePasswordScore;
        public int StoragePasswordScore
        {
            get => _storagePasswordScore;
            set { _storagePasswordScore = value; OnPropertyChanged(nameof(StoragePasswordScore)); }
        }

        private SecureString _storagePassword;
        public SecureString StoragePassword
        {
            get => _storagePassword;
            set
            {
                _storagePassword = value;
                StoragePasswordScore = (int)PasswordAdvisor.CheckStrength(StoragePassword);
                OnPropertyChanged(nameof(StoragePassword));
            }
        }

        private SecureString _storagePasswordConfirmation;
        public SecureString StoragePasswordConfirmation
        {
            get => _storagePasswordConfirmation;
            set { _storagePasswordConfirmation = value; OnPropertyChanged(nameof(StoragePasswordConfirmation)); }
        }

        private HdWallet Wallet { get; set; }

        public CreateNewWalletViewModel()
        {}

        public CreateNewWalletViewModel(IAtomexApp app)
        {
            App = app;

            Network = Atomex.Core.Network.MainNet;
            Language = Languages.FirstOrDefault();
            Entropy = WordCountToEntropyLength.FirstOrDefault();
        }

        public string SaveWalletName()
        {
            if (string.IsNullOrEmpty(WalletName))
            {
                return "Wallet name must be not empty";
            }

            if (WalletName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1 ||
                WalletName.IndexOf('.') != -1)
            {
                return "Invalid wallet name";
            }

            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var walletsFolder = Path.Combine(documents, "..", "Library", WalletInfo.DefaultWalletsDirectory);
            if (!Directory.Exists(walletsFolder))
            {
                Directory.CreateDirectory(walletsFolder);
            }
            var pathToWallet = Path.Combine(walletsFolder, $"{WalletName}", WalletInfo.DefaultWalletFileName);

            try
            {
                var _ = Path.GetFullPath(pathToWallet);
            }
            catch (Exception)
            {
                return "Invalid wallet name";
            }

            if (File.Exists(pathToWallet))
            {
                return "Wallet with the same name already exists";
            }

            PathToWallet = pathToWallet;

            return null;

        }
        public void GenerateMnemonic()
        {
            var entropy = Rand.SecureRandomBytes(Entropy.Length / 8);
            Mnemonic = new Mnemonic(Language.Wordlist, entropy).ToString();
        }

        public string WriteMnemonic()
        {
            if (string.IsNullOrEmpty(Mnemonic))
            {
                return "Mnemonic phrase can not be empty";
            }

            try
            {
                var unused = new Mnemonic(Mnemonic, Language.Wordlist);
                return null;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("Word count should be"))
                    return "Word count should be 12,15,18,21 or 24.";
                else if (e.Message.Contains("is not in the wordlist"))
                    return "Word " + e.Message.Split(' ')[1] + " is not in the wordlist for this language";
                else
                    return "Invalid mnemonic phrase";
            }
        }

        private SecureString GenerateSecureString(string str)
        {
            var secureString = new SecureString();
            foreach (char c in str)
            {
                secureString.AppendChar(c);
            }
            return secureString;
        }

        public void SetPassword(string pswdType, string pswd)
        {
            SecureString secureString = GenerateSecureString(pswd);
            if (pswdType == "StoragePassword")
            {
                StoragePassword = secureString;
                return;
            }
            if (pswdType == "DerivedPassword")
            {
                DerivedPassword = secureString;
                return;
            }
            if (pswdType == "StoragePasswordConfirmation")
            {
                StoragePasswordConfirmation = secureString;
                return;
            }
            if (pswdType == "DerivedPasswordConfirmation")
            {
                DerivedPasswordConfirmation = secureString;
                return;
            }
        }

        public string CheckDerivedPassword()
        {
            if (DerivedPassword != null && DerivedPassword.Length > 0)
            {
                if (DerivedPasswordScore < (int)PasswordAdvisor.PasswordScore.Medium)
                {
                    return "Password has insufficient complexity";
                }

                if (DerivedPasswordConfirmation != null &&
                    !DerivedPassword.SecureEqual(DerivedPasswordConfirmation) || DerivedPasswordConfirmation == null)
                {
                    return "Passwords do not match";
                }
            }
            return null;
        }

        public string CheckStoragePassword()
        {
            if (StoragePasswordScore < (int)PasswordAdvisor.PasswordScore.Medium)
            {
                return "Password has insufficient complexity";
            }

            if (StoragePassword != null &&
                StoragePasswordConfirmation != null &&
                !StoragePassword.SecureEqual(StoragePasswordConfirmation) || StoragePasswordConfirmation == null)
            {
                return "Passwords do not match";
            }
            return null;
        }

        public void CreateHdWallet()
        {
            Wallet = new HdWallet(
                mnemonic: Mnemonic,
                wordList: Language.Wordlist,
                passPhrase: DerivedPassword,
                network: Network)
            {
                PathToWallet = PathToWallet
            };
        }

        public async void ConnectToWallet()
        {
            try
            {

                await Wallet.EncryptAsync(StoragePassword);

                Wallet.SaveToFile(Wallet.PathToWallet, StoragePassword);

                var account = new Account(
                    wallet: Wallet,
                    password: StoragePassword,
                    currenciesProvider: App.CurrenciesProvider,
                    symbolsProvider: App.SymbolsProvider);
                // todo: connect to wallet 
            }
            catch (Exception e)
            {
                // todo: warning
                Log.Error(e, "Create wallet error");
            }
        }

        public void Clear()
        {
            WalletName = string.Empty;
            Language = Languages.FirstOrDefault();
            Entropy = WordCountToEntropyLength.FirstOrDefault();
            Mnemonic = string.Empty;
            DerivedPassword = null;
            DerivedPasswordConfirmation = null;
            DerivedPasswordScore = 0;
            StoragePassword = null;
            StoragePasswordConfirmation = null;
            StoragePasswordScore = 0;
        }
    }
}

