using System;
using System.Collections.Generic;

namespace CustomerRegistration;
public class Company
{
    public string id { get; set; }
    public string phone { get; set; }
    public string cellphone { get; set; }
    public string website { get; set; }
    public string pricingplanid { get; set; }
    public string companyName { get; set; }
    public string companyEmail { get; set; }
    public string address { get; set; }
    public string city { get; set; }
    public string state { get; set; }
    public string zipcode { get; set; }
    public List<string> additionalPlanmembers { get; set; }
    public DateTime creationDate { get; set; }
    public bool success { get; set; } = false;
    public bool isJobSync { get; set; }
    public List<User> listUser { get; set; }
}

public class User
{
    public string id { get; set; }
    public string firstName { get; set; }
    public string lastName { get; set; }
    public string emailAddress { get; set; }
    public string phone { set; get; }
    public string photoUrl { set; get; }
    public string jobtitle { get; set; }
    public bool superUser { get; set; }
}
