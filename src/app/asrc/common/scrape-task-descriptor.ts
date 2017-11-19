import * as moment from 'moment';

export interface IScrapeTaskDescriptor {
    workingDir?: string;
    guidString?: string;
    diaryUrl?: string;
    scrapeStart?: moment.Moment;
    scrapeEnd?: moment.Moment;
    overwrite?: boolean;
    requestDelay?: number;
    progress?: IScrapeTaskProgress;
}

export class ScrapeTaskDescriptor implements IScrapeTaskDescriptor {
    public workingDir: string;
    public guidString: string;
    public diaryUrl: string;
    public scrapeStart: moment.Moment;
    public scrapeEnd: moment.Moment;
    public overwrite: boolean;
    public requestDelay: number;
    public progress: IScrapeTaskProgress;
}

export interface IScrapeTaskProgress {
    currentUrl: string;
    pagesDownloaded: number;
    imagesDownloaded: number;
    bytesDownloaded: number;
    datePagesDiscovered: number;
    datePagesProcessed: number;
}