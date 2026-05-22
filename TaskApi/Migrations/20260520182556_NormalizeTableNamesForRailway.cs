using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskApi.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeTableNamesForRailway : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Comments_Tasks_TaskItemId",
                table: "Comments");

            migrationBuilder.DropForeignKey(
                name: "FK_Projects_ProjectManager_ManagerId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskHistories_Tasks_TaskItemId",
                table: "TaskHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Executors_ExecutorId",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Projects",
                table: "Projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProjectManager",
                table: "ProjectManager");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Executors",
                table: "Executors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Comments",
                table: "Comments");

            migrationBuilder.RenameTable(
                name: "Tasks",
                newName: "tasks");

            migrationBuilder.RenameTable(
                name: "Projects",
                newName: "projects");

            migrationBuilder.RenameTable(
                name: "ProjectManager",
                newName: "projectmanager");

            migrationBuilder.RenameTable(
                name: "Executors",
                newName: "executors");

            migrationBuilder.RenameTable(
                name: "Comments",
                newName: "comments");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_ProjectId",
                table: "tasks",
                newName: "IX_tasks_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_Tasks_ExecutorId",
                table: "tasks",
                newName: "IX_tasks_ExecutorId");

            migrationBuilder.RenameIndex(
                name: "IX_Projects_ManagerId",
                table: "projects",
                newName: "IX_projects_ManagerId");

            migrationBuilder.RenameIndex(
                name: "IX_Comments_TaskItemId",
                table: "comments",
                newName: "IX_comments_TaskItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_tasks",
                table: "tasks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_projects",
                table: "projects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_projectmanager",
                table: "projectmanager",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_executors",
                table: "executors",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_comments",
                table: "comments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_comments_tasks_TaskItemId",
                table: "comments",
                column: "TaskItemId",
                principalTable: "tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_projects_projectmanager_ManagerId",
                table: "projects",
                column: "ManagerId",
                principalTable: "projectmanager",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskHistories_tasks_TaskItemId",
                table: "TaskHistories",
                column: "TaskItemId",
                principalTable: "tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_executors_ExecutorId",
                table: "tasks",
                column: "ExecutorId",
                principalTable: "executors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_tasks_projects_ProjectId",
                table: "tasks",
                column: "ProjectId",
                principalTable: "projects",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_comments_tasks_TaskItemId",
                table: "comments");

            migrationBuilder.DropForeignKey(
                name: "FK_projects_projectmanager_ManagerId",
                table: "projects");

            migrationBuilder.DropForeignKey(
                name: "FK_TaskHistories_tasks_TaskItemId",
                table: "TaskHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_executors_ExecutorId",
                table: "tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_tasks_projects_ProjectId",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_tasks",
                table: "tasks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_projects",
                table: "projects");

            migrationBuilder.DropPrimaryKey(
                name: "PK_projectmanager",
                table: "projectmanager");

            migrationBuilder.DropPrimaryKey(
                name: "PK_executors",
                table: "executors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_comments",
                table: "comments");

            migrationBuilder.RenameTable(
                name: "tasks",
                newName: "Tasks");

            migrationBuilder.RenameTable(
                name: "projects",
                newName: "Projects");

            migrationBuilder.RenameTable(
                name: "projectmanager",
                newName: "ProjectManager");

            migrationBuilder.RenameTable(
                name: "executors",
                newName: "Executors");

            migrationBuilder.RenameTable(
                name: "comments",
                newName: "Comments");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_ProjectId",
                table: "Tasks",
                newName: "IX_Tasks_ProjectId");

            migrationBuilder.RenameIndex(
                name: "IX_tasks_ExecutorId",
                table: "Tasks",
                newName: "IX_Tasks_ExecutorId");

            migrationBuilder.RenameIndex(
                name: "IX_projects_ManagerId",
                table: "Projects",
                newName: "IX_Projects_ManagerId");

            migrationBuilder.RenameIndex(
                name: "IX_comments_TaskItemId",
                table: "Comments",
                newName: "IX_Comments_TaskItemId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tasks",
                table: "Tasks",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Projects",
                table: "Projects",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProjectManager",
                table: "ProjectManager",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Executors",
                table: "Executors",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Comments",
                table: "Comments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Comments_Tasks_TaskItemId",
                table: "Comments",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_ProjectManager_ManagerId",
                table: "Projects",
                column: "ManagerId",
                principalTable: "ProjectManager",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaskHistories_Tasks_TaskItemId",
                table: "TaskHistories",
                column: "TaskItemId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Executors_ExecutorId",
                table: "Tasks",
                column: "ExecutorId",
                principalTable: "Executors",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Projects_ProjectId",
                table: "Tasks",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id");
        }
    }
}
