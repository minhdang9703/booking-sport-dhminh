insert into "Users" (
    "Id",
    "FullName",
    "Email",
    "PasswordHash",
    "PhoneNumber",
    "Role",
    "CreatedAt",
    "UpdatedAt",
    "DeletedAt"
)
values
(
    '00000000-0000-0000-0000-000000000101',
    'Seed Customer',
    'user01',
    'AQAAAAIAAYagAAAAEM1FFNlscKaYKEoV5Wg/ECvhrHqBITazEPQL6TpFu43HUDOj+DzYvfzXOyqRcDQYYQ==',
    null,
    'Customer',
    now(),
    now(),
    null
),
(
    '00000000-0000-0000-0000-000000000201',
    'Seed Admin',
    'admin',
    'AQAAAAIAAYagAAAAEGLabC21wsPnd9f1C4zbuDgdBLzTNBniDqNrtbam/of1dBws6GiHDuBp02X//GJwjw==',
    null,
    'Admin',
    now(),
    now(),
    null
)
on conflict ("Email") do update set
    "FullName" = excluded."FullName",
    "PasswordHash" = excluded."PasswordHash",
    "PhoneNumber" = excluded."PhoneNumber",
    "Role" = excluded."Role",
    "UpdatedAt" = now(),
    "DeletedAt" = null;
