<?php

namespace App\Http\Clients;

use Illuminate\Support\Facades\Http;
use Illuminate\Support\Facades\Storage;

class GraphQlClient
{
    public function __construct(
        protected ?string $url,
        protected ?string $authToken,
        protected array $operations = []
    ) {
        $this->url = ($url ?? config('platform.url')).'/graphql/';
        $this->authToken = $authToken ?? config('platform.auth_token');
        $this->loadOperations();
    }

    /**
     * Takes an operation name and variables and returns the response.
     * The operation name is the name of a file in the graphql storage
     * directory which contains the actual operation.
     *
     * @return array|null
     *
     * @throws \Exception
     */
    public function graphQl(string $operationName, ...$variables): mixed
    {
        [$schema, $operation] = $this->getOperation($operationName);
        $schema = $schema === config('platform.primary_schema') ? '' : "{$schema}";

        return $this->graphQlRaw($schema, $operation, ...$variables)[$operationName] ?? null;
    }

    /**
     * Takes a schema name, a raw operation string, and variables and returns the response.
     *
     * @return array|null
     *
     * @throws \Illuminate\Http\Client\ConnectionException
     */
    public function graphQlRaw(string $schema, string $operation, ...$variables): mixed
    {
        $response = Http::withHeaders(['Authorization' => "{$this->authToken}"])
            ->post($this->url.$schema, [
                'query' => $operation,
                'variables' => $variables,
            ]);

        return $response->json()['data'] ?? null;
    }

    /**
     * Get the operation from the operations array.
     *
     * @throws \Exception
     */
    protected function getOperation(string $operationName): ?array
    {
        $matchingOperation = collect(array_filter(array_keys($this->operations), fn ($operation) => str_ends_with($operation, ".$operationName")))->first();

        if (! empty($matchingOperation)) {
            $operation = $this->operations[$matchingOperation];
        } else {
            throw new \Exception("Operation {$operationName} not found.");
        }

        [$schema, $operationType, $operationName] = explode('.', $matchingOperation);

        return [$schema, Storage::disk('graphql')->get($operation)];
    }

    /**
     * Load all query names and paths from the graphql storage directory.
     */
    protected function loadOperations(): void
    {
        $files = Storage::disk('graphql')->allFiles();

        collect($files)
            ->filter(fn ($file) => str_ends_with($file, '.gql') || str_ends_with($file, '.graphql'))
            ->each(
                function ($file): void {
                    $filename = pathinfo($file, PATHINFO_FILENAME);
                    [$root, $schema, $operation] = explode('/', pathinfo($file)['dirname']);
                    $this->operations["{$schema}.{$operation}.{$filename}"] = $file;
                }
            );
    }
}
