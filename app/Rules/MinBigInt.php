<?php

namespace App\Rules;

use Closure;
use Illuminate\Contracts\Validation\ValidationRule;

class MinBigInt implements ValidationRule
{
    /**
     * The validation error message.
     */
    protected string $message;

    /**
     * Create a new rule instance.
     */
    public function __construct(protected string|int $min = 0) {}

    /**
     * Determine if the validation rule passes.
     *
     * @param  Closure(string): \Illuminate\Translation\PotentiallyTranslatedString  $fail
     */
    public function validate(string $attribute, mixed $value, Closure $fail): void
    {
        if (! (is_array($value) ? collect($value)->flatten()->every(fn ($item) => $this->isValidMinBigInt($item)) : $this->isValidMinBigInt($value))) {
            $fail($this->message)
                ->translate([
                    'min' => $this->min,
                ]);
        }
    }

    /**
     * Determine if the value is a valid min big int.
     */
    protected function isValidMinBigInt($value): bool
    {
        if (! is_numeric($value)) {
            $this->message = 'validation.numeric';

            return false;
        }

        $this->message = 'validation.min_big_int';

        return bccomp($this->min, $value) <= 0;
    }
}
