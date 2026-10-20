using System.ComponentModel.DataAnnotations;
using CareerSyncAI.Services;

namespace CareerSyncAI.Models.ViewModels;

[AttributeUsage(AttributeTargets.Property)]
public sealed class SmsPhoneNumberAttribute : ValidationAttribute
{
    public SmsPhoneNumberAttribute()
        : base("Enter a mobile number like 082 123 4567, or include the country code (e.g. +44 7700 900123).") { }

    public override bool IsValid(object? value) =>
        value is not string s || string.IsNullOrWhiteSpace(s) || PhoneNumbers.ToE164(s) is not null;
}
