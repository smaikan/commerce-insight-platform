import hmac, hashlib
secret = b"sandbox-m55Tks9XnWfpUA9WImFhmrPKZMOta2vi"
fields = {
    "paymentStatus": "SUCCESS",
    "paymentId": "37402526",
    "currency": "TRY",
    "basketId": "2a19c74030284885ac58c1c2a71a7dbb",
    "paidPrice": "599",
    "price": "374.17",
    "token": "29a3182d-6dc2-4d52-ac40-6f114909595c"
}
target = "1d1598c0e752e2703f432782c47df5ef51d750635770d36b8082938e9e3df355"

combos = [
    ("Omit ConversationId", ':'.join([fields["paymentStatus"], fields["paymentId"], fields["currency"], fields["basketId"], fields["paidPrice"], fields["price"], fields["token"]])),
    ("Empty ConversationId (::)", ':'.join([fields["paymentStatus"], fields["paymentId"], fields["currency"], fields["basketId"], "", fields["paidPrice"], fields["price"], fields["token"]])),
    ("With hostReference", ':'.join([fields["paymentStatus"], fields["paymentId"], fields["currency"], fields["basketId"], "mock00001iyzihostrfn", fields["paidPrice"], fields["price"], fields["token"]])),
    ("Omit ConversationId, no decimals", ':'.join([fields["paymentStatus"], fields["paymentId"], fields["currency"], fields["basketId"], "599", "374", fields["token"]])),
    ("Empty ConversationId (::), string zeros", ':'.join([fields["paymentStatus"], fields["paymentId"], fields["currency"], fields["basketId"], "", "599.00", "374.17", fields["token"]])),
]

print("Target:", target)
for name, combo in combos:
    h = hmac.new(secret, combo.encode('utf-8'), hashlib.sha256).hexdigest()
    if h == target:
        print("MATCH FOUND!!!", name)
        print(combo)
