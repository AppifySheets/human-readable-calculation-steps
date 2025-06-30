using HumanReadableCalculationSteps;

var originalPrice = 100m;
var discount = 15m;

var basePrice = ValueWithCaption.From(() => originalPrice);
var discountAmount = ValueWithCaption.From(() => discount);

var discountedPrice = (basePrice - discountAmount).As("DiscountedPrice");
var taxRate = ValueWithCaption.From(() => 0.18m);
var finalPrice = discountedPrice + discountedPrice * taxRate;

Console.WriteLine("discountedPrice.FinalCalculationSteps:");
Console.WriteLine("\"" + discountedPrice.FinalCalculationSteps + "\"");
Console.WriteLine();
Console.WriteLine("finalPrice.FinalCalculationSteps:");
Console.WriteLine("\"" + finalPrice.FinalCalculationSteps + "\"");
