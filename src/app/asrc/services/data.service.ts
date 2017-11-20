import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { DiaryScraperInputData } from '../common/diary-scraper-input-data';

@Injectable()
export class DataService {

    private inputData = new BehaviorSubject<DiaryScraperInputData>(new DiaryScraperInputData());

    currentData = this.inputData.asObservable();

    constructor() { }

    changeData(newData: DiaryScraperInputData) {
        this.inputData.next(newData);
    }
}