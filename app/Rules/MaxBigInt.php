<?php

namespace App\Rules;

use Closure;
use Enjin\BlockchainTools\HexNumber\HexInt\HexInt256;
use Illuminate\Contracts\Validation\ValidationRule;

class MaxBigInt implements ValidationRule
{
    /**
     * The validation error message.
     */
    protected string $message;

    /**
     * Create a new rule instance.
     */
    public function __construct(protected string|int $max = HexInt256::INT_SIZE) {}

    /**
     * Determine if the validation rule passes.
     *
     * @param  Closure(string): \Illuminate\Translation\PotentiallyTranslatedString  $fail
     */
    public function validate(string $attribute, mixed $value, Closure $fail): void
    {
        if (! (is_array($value) ? collect($value)->flatten()->every(fn ($item) => $this->isValidMaxBigInt($item)) : $this->isValidMaxBigInt($value))) {
            $fail($this->message)
                ->translate([
                    'max' => $this->max,
                ]);
        }
    }

    /**
     * Determine if the value is a valid max big int.
     */
    protected function isValidMaxBigInt($value): bool
    {
        if (! is_numeric($value)) {
            $this->message = 'validation.numeric';

            return false;
        }

        $this->message = 'validation.max_big_int';

        return bccomp($this->max, $value) >= 0;
    }
}
