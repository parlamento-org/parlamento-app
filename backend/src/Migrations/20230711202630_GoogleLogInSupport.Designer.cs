﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using backend.Models;

#nullable disable

namespace backend.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20230711202630_GoogleLogInSupport")]
    partial class GoogleLogInSupport
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.8");

            modelBuilder.Entity("backend.Models.PartyStats", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("PartyAffection")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PoliticalPartypartyAcronym")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("UserId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("PoliticalPartypartyAcronym");

                    b.HasIndex("UserId");

                    b.ToTable("PartyStats");
                });

            modelBuilder.Entity("backend.Models.PoliticalParty", b =>
                {
                    b.Property<string>("partyAcronym")
                        .HasColumnType("TEXT");

                    b.Property<string>("fullName")
                        .HasColumnType("TEXT");

                    b.HasKey("partyAcronym");

                    b.ToTable("PoliticalParties");

                    b.HasData(
                        new
                        {
                            partyAcronym = "PSD",
                            fullName = "Partido Social-Democrata"
                        },
                        new
                        {
                            partyAcronym = "PS",
                            fullName = "Partido Socialista"
                        },
                        new
                        {
                            partyAcronym = "BE",
                            fullName = "Bloco de Esquerda"
                        },
                        new
                        {
                            partyAcronym = "PCP",
                            fullName = "Partido Comunista Português"
                        },
                        new
                        {
                            partyAcronym = "CDSPP",
                            fullName = "Coligação Democrática Unitária"
                        },
                        new
                        {
                            partyAcronym = "PAN",
                            fullName = "Pessoas-Animais-Natureza"
                        },
                        new
                        {
                            partyAcronym = "L",
                            fullName = "Partido Livre"
                        },
                        new
                        {
                            partyAcronym = "IL",
                            fullName = "Iniciativa Liberal"
                        },
                        new
                        {
                            partyAcronym = "CH",
                            fullName = "Chega"
                        },
                        new
                        {
                            partyAcronym = "Governo",
                            fullName = "Governo"
                        });
                });

            modelBuilder.Entity("backend.Models.ProjectLaw", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("FullProposalTextLink")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Legislatura")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("ProposalResult")
                        .HasColumnType("INTEGER");

                    b.Property<string>("ProposalTextHTML")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProposalTitle")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ProposingPartypartyAcronym")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Score")
                        .HasColumnType("INTEGER");

                    b.Property<int>("SourceId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("VoteDate")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int?>("VotingResultGeneralityId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("VotingResultSpecialityId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProposingPartypartyAcronym");

                    b.HasIndex("VotingResultGeneralityId");

                    b.HasIndex("VotingResultSpecialityId");

                    b.ToTable("ProjectLaws");
                });

            modelBuilder.Entity("backend.Models.User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ProfilePic")
                        .HasColumnType("INTEGER");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("googleAccessToken")
                        .HasColumnType("TEXT");

                    b.Property<string>("googleIDToken")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("backend.Models.Vote", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProjectLawId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("UserId")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("VoteDate")
                        .HasColumnType("TEXT");

                    b.Property<int>("VotingOrientation")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProjectLawId");

                    b.HasIndex("UserId");

                    b.ToTable("Vote");
                });

            modelBuilder.Entity("backend.Models.VotingBlock", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int?>("VotingResultId")
                        .HasColumnType("INTEGER");

                    b.Property<bool?>("isUninamousWithinParty")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("numberOfDeputies")
                        .HasColumnType("INTEGER");

                    b.Property<string>("politicalPartyAcronym")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("votingOrientation")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("VotingResultId");

                    b.ToTable("VotingBlock");
                });

            modelBuilder.Entity("backend.Models.VotingResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<bool>("isUninamous")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.ToTable("VotingResult");
                });

            modelBuilder.Entity("backend.Models.PartyStats", b =>
                {
                    b.HasOne("backend.Models.PoliticalParty", "PoliticalParty")
                        .WithMany()
                        .HasForeignKey("PoliticalPartypartyAcronym")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("backend.Models.User", null)
                        .WithMany("PartyStats")
                        .HasForeignKey("UserId");

                    b.Navigation("PoliticalParty");
                });

            modelBuilder.Entity("backend.Models.ProjectLaw", b =>
                {
                    b.HasOne("backend.Models.PoliticalParty", "ProposingParty")
                        .WithMany()
                        .HasForeignKey("ProposingPartypartyAcronym")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("backend.Models.VotingResult", "VotingResultGenerality")
                        .WithMany()
                        .HasForeignKey("VotingResultGeneralityId");

                    b.HasOne("backend.Models.VotingResult", "VotingResultSpeciality")
                        .WithMany()
                        .HasForeignKey("VotingResultSpecialityId");

                    b.Navigation("ProposingParty");

                    b.Navigation("VotingResultGenerality");

                    b.Navigation("VotingResultSpeciality");
                });

            modelBuilder.Entity("backend.Models.Vote", b =>
                {
                    b.HasOne("backend.Models.ProjectLaw", "ProjectLaw")
                        .WithMany()
                        .HasForeignKey("ProjectLawId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("backend.Models.User", null)
                        .WithMany("Votes")
                        .HasForeignKey("UserId");

                    b.Navigation("ProjectLaw");
                });

            modelBuilder.Entity("backend.Models.VotingBlock", b =>
                {
                    b.HasOne("backend.Models.VotingResult", null)
                        .WithMany("votingBlocks")
                        .HasForeignKey("VotingResultId");
                });

            modelBuilder.Entity("backend.Models.User", b =>
                {
                    b.Navigation("PartyStats");

                    b.Navigation("Votes");
                });

            modelBuilder.Entity("backend.Models.VotingResult", b =>
                {
                    b.Navigation("votingBlocks");
                });
#pragma warning restore 612, 618
        }
    }
}
