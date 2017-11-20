import { Directive } from "@angular/core";
import { NG_VALIDATORS, Validator, AbstractControl, ValidatorFn } from "@angular/forms";
import * as moment from 'moment';

@Directive({
    selector: '[momentValidateExact]',
    providers: [{ provide: NG_VALIDATORS, useExisting: MomentValidateExactDirective, multi: true }]
})
export class MomentValidateExactDirective implements Validator {

    validate(control: AbstractControl): { [key: string]: any } {
        return momentDateValidator()(control);
    }
}

export function momentDateValidator(): ValidatorFn {
    return (control: AbstractControl): { [key: string]: any } => {
        let momentValue = control.value as moment.Moment;
        if (momentValue === null) {
            return null;
        }
        let checkParse = moment((momentValue as any)._i, (momentValue as any)._f, true);
        if (checkParse.isValid()) {
            return null;
        }
        return { 'momentValidateExact': { format: (momentValue as any)._f, value: (momentValue as any)._i } };
    };
}