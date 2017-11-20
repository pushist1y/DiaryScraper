import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DiaryInputComponent } from './diaryinput.component'
import { DiaryProgressComponent } from './diary-progress.component';

const routes: Routes = [
  { path: '', redirectTo: '/input', pathMatch: 'full' },
  { path: 'input', component: DiaryInputComponent },
  { path: 'progress', component: DiaryProgressComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }