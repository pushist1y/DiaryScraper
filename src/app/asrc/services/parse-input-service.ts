import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { DiaryParserInputData } from '../common/diary-parser-input-data';

@Injectable()
export class ParseInputDataService {

    private inputData = new BehaviorSubject<DiaryParserInputData>(new DiaryParserInputData());

    currentData = this.inputData.asObservable();

    constructor() { }

    changeData(newData: DiaryParserInputData) {
        this.inputData.next(newData);
    }
}
