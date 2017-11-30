import * as moment from 'moment';

export class DiaryScraperInputData {
    public diaryLogin: string = "";
    public diaryPassword: string = "";
    public diaryAddress: string = "";
    public workingDir: string = "";
    public dateStart: DateCheck;
    public dateEnd: DateCheck;
    public overwrite: boolean = false;
    public requestDelay: number = 1000;
    public downloadEdits: boolean = false;
  
    constructor() {
      this.dateStart = new DateCheck();
      this.dateStart.value = moment([2000, 0, 1]);
      this.dateEnd = new DateCheck();
      this.dateEnd.value = moment([2025, 0, 1]);
    }
  }
  
  export class DateCheck {
    public value: moment.Moment = moment();
    public enabled: boolean = false;
  }