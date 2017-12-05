import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DiaryInputComponent } from './diaryinput.component'
import { DiaryProgressComponent } from './diary-progress.component';
import { DiaryParseInputComponent } from './diary-parse-input.component';
import { DiaryParseProgressComponent } from './diary-parse-progress.component';
import { DiaryArchiveInputComponent } from './diary-archive-input.component';
import { DiaryArchiveProgressComponent } from './diary-archive-progress.component';

const routes: Routes = [
  { path: '', redirectTo: '/input', pathMatch: 'full' },
  { path: 'input', component: DiaryInputComponent },
  { path: 'progress', component: DiaryProgressComponent },
  { path: 'parse', component: DiaryParseInputComponent },
  { path: 'parseprogress', component: DiaryParseProgressComponent },
  { path: 'archive', component: DiaryArchiveInputComponent },
  { path: 'archiveprogress', component: DiaryArchiveProgressComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }