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
        return $this->graphQlClient->graphQl('GetManagedWallet', externalId: $externalId);
    }

    public function createManagedWalletAccount(string $externalId)
    {
        return $this->graphQlClient->graphQl('CreateManagedWallet', externalId: $externalId);
    }
}
