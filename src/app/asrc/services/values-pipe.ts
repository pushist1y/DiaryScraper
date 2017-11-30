import { PipeTransform, Pipe } from "@angular/core";

@Pipe({ name: 'values', pure: false })
export class ValuesPipe implements PipeTransform {
    transform(value: any, args: any[] = null): any {
        if (!value) {
            return [] as Array<any>;
        }
        return Object.keys(value).map(key => { return { key, value: value[key] } });
    }
}