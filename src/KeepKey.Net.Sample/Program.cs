﻿using Hid.Net;
using KeepKey.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trezor.Manager;

namespace TrezorTestApp
{
    class Program
    {
        #region Fields
        private static readonly string[] _Addresses = new string[50];
        #endregion

        #region Main
        static void Main(string[] args)
        {
            try
            {
                Go();
                while (true) ;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
        #endregion

        #region Private  Methods
        private static async Task<IHidDevice> Connect()
        {
            var devices = WindowsHidDevice.GetConnectedDeviceInformations();
            var keepKeyDeviceInformation = devices.FirstOrDefault(d => d.VendorId == 11044 && d.ProductId == 1);

            if (keepKeyDeviceInformation == null)
            {
                throw new Exception("No KeepKey is not connected or USB access was not granted to this application.");
            }

            var keepKeyHidDevice = new WindowsHidDevice(keepKeyDeviceInformation);

            keepKeyHidDevice.DataHasExtraByte = false;

            await keepKeyHidDevice.InitializeAsync();

            return keepKeyHidDevice;
        }

        /// <summary>
        /// TODO: This should be made in to a unit test but it's annoying to add the UI for a unit test as the Trezor requires human intervention for the pin
        /// </summary>
        /// <returns></returns>
        private async static Task Go()
        {
            using (var trezorHid = await Connect())
            {
                using (var trezorManager = new KeepKeyManager(GetPin, trezorHid))
                {
                    await trezorManager.InitializeAsync();

                    var tasks = new List<Task>();

                    for (var i = 0; i < 50; i++)
                    {
                        tasks.Add(DoGetAddress(trezorManager, i));
                    }

                    await Task.WhenAll(tasks);

                    for (var i = 0; i < 50; i++)
                    {
                        var address = await GetAddress(trezorManager, i);

                        Console.WriteLine($"Index: {i} (No change) - Address: {address}");

                        if (address != _Addresses[i])
                        {
                            throw new Exception("The ordering got messed up");
                        }
                    }

                    Console.WriteLine("All good");

                    Console.ReadLine();
                }
            }
        }

        private async static Task DoGetAddress(TrezorManagerBase trezorManager, int i)
        {
            var address = await GetAddress(trezorManager, i);
            _Addresses[i] = address;
        }

        private static async Task<string> GetAddress(TrezorManagerBase trezorManager, int i)
        {
            return await trezorManager.GetAddressAsync("BTC", 0, 0, false, (uint)i, false, AddressType.Bitcoin, false);
        }

        private async static Task<string> GetPin()
        {
            Console.WriteLine("Enter PIN based on Trezor values: ");
            return Console.ReadLine().Trim();
        }
        #endregion
    }
}