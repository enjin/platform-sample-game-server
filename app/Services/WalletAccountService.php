<?php

namespace App\Services;

use App\Http\Clients\GraphQlClient;

class WalletAccountService
{
    public function __construct(protected GraphQlClient $graphQlClient) {}

    public function getWalletAccount() {}

    public function getWalletAccounts() {}

    public function getManagedWalletAccount(string $externalId)
    {
        $wallet = $this->graphQlClient->graphQl('GetManagedWallet', externalId: $externalId);

        ray($wallet);

        if (! $wallet) {
            return null;
        }

        $tokenAccounts = collect($wallet['tokenAccounts']['edges'] ?? [])->map(function ($edge) {
            $tokenAccount = $edge['node'];

            return [
                'balance' => $tokenAccount['balance'],
                'token' => [
                    'tokenId' => $tokenAccount['token']['tokenId'],
                    'name' => $tokenAccount['token']['tokenMetadata']['name'],
                ],
            ];
        });

        return [
            'address' => $wallet['account']['address'],
            'tokens' => $tokenAccounts,
        ];
    }

    public function createManagedWalletAccount(string $externalId)
    {
        return $this->graphQlClient->graphQl('CreateManagedWallet', externalId: $externalId);
    }
}
